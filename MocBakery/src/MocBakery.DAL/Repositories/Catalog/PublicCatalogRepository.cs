using System.Data;
using System.Data.Common;
using MocBakery.DAL.Repositories.Inventory;
using MocBakery.Models.PublicCatalog;
namespace MocBakery.DAL.Repositories.Catalog;

public sealed class PublicCatalogRepository(DbConnectionFactory db) : IPublicCatalogRepository
{
    private const string Visible="p.DangKinhDoanh=1 AND c.DangSuDung=1";
    private const string Fields="p.MaSanPham,p.MaLoai,p.TenSanPham,c.TenLoai,p.DonViTinh,p.GiaBan,p.MoTa,CONVERT(bit,CASE WHEN DATALENGTH(p.AnhBanh)>0 THEN 1 ELSE 0 END),COALESCE(stock.Quantity,0)";
    private static readonly string From=$"""
        FROM dbo.SanPham p JOIN dbo.LoaiBanh c ON c.MaLoai=p.MaLoai
        OUTER APPLY(SELECT SUM(CONVERT(bigint,l.SoLuongCon)) Quantity FROM dbo.LoBanh l
            WHERE l.MaSanPham=p.MaSanPham AND {LoBanhRepository.SellableLotCondition}) stock
        """;

    public IReadOnlyList<PublicCategory> Categories()
    {
        using var c=db.Open();using var q=Db.Command(c,"""
            SELECT c.MaLoai,c.TenLoai,COUNT(p.MaSanPham)
            FROM dbo.LoaiBanh c LEFT JOIN dbo.SanPham p ON p.MaLoai=c.MaLoai AND p.DangKinhDoanh=1
            WHERE c.DangSuDung=1 GROUP BY c.MaLoai,c.TenLoai ORDER BY c.TenLoai,c.MaLoai
            """);
        using var r=q.ExecuteReader();List<PublicCategory> result=[];
        while(r.Read())result.Add(new(r.GetInt32(0),r.GetString(1),r.GetInt32(2)));
        return result;
    }

    public CatalogSnapshotPage Browse(PublicCatalogQuery input)
    {
        string sort=input.Sort switch{"price-asc"=>"p.GiaBan,p.MaSanPham","price-desc"=>"p.GiaBan DESC,p.MaSanPham",_=>"p.TenSanPham,p.MaSanPham"};
        string availability=input.Availability switch{"in-stock"=>" AND COALESCE(stock.Quantity,0)>0","out-of-stock"=>" AND COALESCE(stock.Quantity,0)=0",_=>""};
        const string filter=" AND (@category IS NULL OR p.MaLoai=@category) AND (@search=N'' OR p.TenSanPham LIKE @key ESCAPE '~' OR p.MaSanPham=@code)";
        using var c=db.Open();using var q=Db.Command(c,$"""
            SELECT COUNT(*) {From} WHERE {Visible}{filter}{availability};
            SELECT {Fields} {From} WHERE {Visible}{filter}{availability}
            ORDER BY {sort} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
            """);
        string search=input.Search??"",escaped=search.Replace("~","~~").Replace("%","~%").Replace("_","~_").Replace("[","~[");
        string codeText=search.StartsWith('B')||search.StartsWith('b')?search[1..]:search;
        int code=int.TryParse(codeText,out int id)&&id>0?id:0;
        q.Param("@search",search,DbType.String,100);q.Param("@key","%"+escaped+"%",DbType.String,500);q.Param("@code",code,DbType.Int32);
        q.Param("@category",input.CategoryId,DbType.Int32);q.Param("@skip",(long)(input.Page-1)*input.PageSize,DbType.Int64);q.Param("@take",input.PageSize,DbType.Int32);
        q.Param("@day",Clock(c),DbType.Date);
        using var r=q.ExecuteReader();r.Read();int total=r.GetInt32(0);r.NextResult();List<CatalogProductSnapshot> rows=[];
        while(r.Read())rows.Add(Read(r));
        return new(rows,total);
    }

    public CatalogProductSnapshot? Find(int id)
    {
        using var c=db.Open();using var q=Db.Command(c,$"SELECT {Fields} {From} WHERE {Visible} AND p.MaSanPham=@id");
        q.Param("@id",id,DbType.Int32);q.Param("@day",Clock(c),DbType.Date);using var r=q.ExecuteReader();return r.Read()?Read(r):null;
    }

    public byte[]? Image(int id)
    {
        using var c=db.Open();using var q=Db.Command(c,$"SELECT p.AnhBanh FROM dbo.SanPham p JOIN dbo.LoaiBanh c ON c.MaLoai=p.MaLoai WHERE {Visible} AND p.MaSanPham=@id");
        q.Param("@id",id,DbType.Int32);return q.ExecuteScalar() as byte[];
    }

    private static DateTime Clock(DbConnection c){using var q=Db.Command(c,"SELECT CONVERT(date,SYSDATETIME())");return (DateTime)q.ExecuteScalar()!;}
    private static CatalogProductSnapshot Read(DbDataReader r)=>new(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetDecimal(5),r.IsDBNull(6)?null:r.GetString(6),r.GetBoolean(7),r.GetInt64(8));
}
