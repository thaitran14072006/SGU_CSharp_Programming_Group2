using System.Data;
using System.Data.Common;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Inventory;
namespace MocBakery.DAL.Repositories.Inventory;
public sealed class LoBanhRepository(DbConnectionFactory db) : ILoBanhRepository
{
    private const string Columns = "l.MaLo,l.MaSanPham,l.NgayNhap,l.NguonNhap,l.NgaySanXuat,l.HanSuDung,l.SoLuongNhap,l.SoLuongCon,l.TinhTrang";
    public IReadOnlyList<LoBanh> List()
    {
        using var c = db.Open(); using var q = Db.Command(c,$"SELECT {Columns} FROM dbo.LoBanh l ORDER BY l.HanSuDung,l.MaLo");
        return Read(q);
    }
    public int Receive(LotInput input)
    {
        using var c = db.Open();
        // Lock the product during insertion so concurrent deactivation cannot bypass the active check.
        using var tx = c.BeginTransaction();
        using var q = Db.Command(c,"""
            INSERT dbo.LoBanh(MaSanPham,NguonNhap,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon,TinhTrang)
            OUTPUT INSERTED.MaLo
            SELECT MaSanPham,@source,@produced,@expiry,@qty,@qty,N'DangBan'
            FROM dbo.SanPham WITH (UPDLOCK,HOLDLOCK) WHERE MaSanPham=@product AND DangKinhDoanh=1
            """,tx);
        q.Param("@product",input.MaSanPham,DbType.Int32); q.Param("@source",input.NguonNhap,DbType.String,100);
        q.Param("@produced",input.NgaySanXuat.ToDateTime(TimeOnly.MinValue),DbType.Date);
        q.Param("@expiry",input.HanSuDung.ToDateTime(TimeOnly.MinValue),DbType.Date);
        q.Param("@qty",input.SoLuongNhap,DbType.Int32);
        object? id = q.ExecuteScalar();
        if (id is null) throw new BusinessException("Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.","Product");
        tx.Commit(); return Convert.ToInt32(id);
    }
    public void SetStatus(int id, string status)
    {
        using var c = db.Open(); using var q = Db.Command(c,"UPDATE dbo.LoBanh SET TinhTrang=@status WHERE MaLo=@id");
        q.Param("@id",id,DbType.Int32); q.Param("@status",status,DbType.String,20);
        if (q.ExecuteNonQuery() != 1) throw new BusinessException("Lô bánh không còn tồn tại. Hãy tải lại.");
    }
    public IReadOnlyList<LoBanh> GetSellableLots(int productId, DateOnly businessDate)
    {
        using var c = db.Open(); return ReadSellableLots(c,null,productId,businessDate);
    }
    // Sales must call this overload inside its transaction, then use conditional stock updates.
    // This method never commits or subtracts stock. It is a lookup, not a reservation.
    public IReadOnlyList<LoBanh> GetSellableLots(DbConnection connection, DbTransaction? transaction,
        int productId, DateOnly businessDate) => ReadSellableLots(connection, transaction, productId, businessDate);

    internal static IReadOnlyList<LoBanh> ReadSellableLots(DbConnection connection, DbTransaction? transaction,
        int productId, DateOnly businessDate)
    {
        using var q = Db.Command(connection,$"""
            SELECT {Columns} FROM dbo.LoBanh l
            JOIN dbo.SanPham p ON p.MaSanPham=l.MaSanPham
            WHERE l.MaSanPham=@product AND p.DangKinhDoanh=1 AND l.TinhTrang=N'DangBan'
              AND l.SoLuongCon>0 AND l.NgaySanXuat<=@date AND l.HanSuDung>=@date
            ORDER BY l.HanSuDung,l.MaLo
            """,transaction);
        q.Param("@product",productId,DbType.Int32);
        q.Param("@date",businessDate.ToDateTime(TimeOnly.MinValue),DbType.Date);
        return Read(q);
    }
    private static IReadOnlyList<LoBanh> Read(DbCommand q)
    {
        using var r = q.ExecuteReader(); List<LoBanh> rows = [];
        while (r.Read()) rows.Add(new(r.GetInt32(0),r.GetInt32(1),r.GetDateTime(2),r.IsDBNull(3)?null:r.GetString(3),
            DateOnly.FromDateTime(r.GetDateTime(4)),DateOnly.FromDateTime(r.GetDateTime(5)),r.GetInt32(6),r.GetInt32(7),r.GetString(8)));
        return rows;
    }
}
