using System.Data;
using System.Data.Common;
using MocBakery.Models;
using MocBakery.Models.Entities;
namespace MocBakery.DAL.Repositories.Catalog;
public sealed class SanPhamRepository(DbConnectionFactory db) : ISanPhamRepository
{
    public IReadOnlyList<SanPham> List()
    {
        using var c = db.Open();
        using var q = Db.Command(c,"SELECT MaSanPham,MaLoai,TenSanPham,DonViTinh,GiaBan,DangKinhDoanh,MoTa,HanDungNgay,AnhBanh,NguongCanhBao FROM dbo.SanPham ORDER BY TenSanPham,MaSanPham");
        using var r = q.ExecuteReader(); List<SanPham> rows = [];
        while (r.Read()) rows.Add(new(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetString(3),r.GetDecimal(4),r.GetBoolean(5),r.IsDBNull(6)?null:r.GetString(6),r.GetInt32(7),r.IsDBNull(8)?null:(byte[])r.GetValue(8),r.GetDecimal(9)));
        return rows;
    }
    public int Save(SanPham input)
    {
        using var c = db.Open();
        using var q = Db.Command(c,input.MaSanPham == 0
            ? "INSERT dbo.SanPham(MaLoai,TenSanPham,DonViTinh,GiaBan,DangKinhDoanh,MoTa,HanDungNgay,AnhBanh,NguongCanhBao) OUTPUT INSERTED.MaSanPham VALUES(@category,@name,@unit,@price,@active,@note,@shelf,@image,@threshold)"
            : "UPDATE dbo.SanPham SET MaLoai=@category,TenSanPham=@name,DonViTinh=@unit,GiaBan=@price,DangKinhDoanh=@active,MoTa=@note,HanDungNgay=@shelf,AnhBanh=@image,NguongCanhBao=@threshold OUTPUT INSERTED.MaSanPham WHERE MaSanPham=@id");
        q.Param("@shelf",input.HanDungNgay,DbType.Int32);q.Param("@image",input.AnhBanh,DbType.Binary,-1);
        q.Param("@threshold",input.NguongCanhBao,DbType.Decimal);q.Parameters["@threshold"].Precision=18;q.Parameters["@threshold"].Scale=3;
        q.Param("@category",input.MaLoai,DbType.Int32); q.Param("@name",input.TenSanPham,DbType.String,150);
        q.Param("@unit",input.DonViTinh,DbType.String,20); q.Param("@price",input.GiaBan,DbType.Decimal);
        q.Parameters["@price"].Precision = 18; q.Parameters["@price"].Scale = 2;
        q.Param("@active",input.DangKinhDoanh,DbType.Boolean); q.Param("@note",input.MoTa,DbType.String,500);
        if (input.MaSanPham != 0) q.Param("@id",input.MaSanPham,DbType.Int32);
        try { return Convert.ToInt32(q.ExecuteScalar() ?? throw new BusinessException("Sản phẩm không còn tồn tại. Hãy tải lại.")); }
        catch (DbException ex) when (Db.IsDuplicate(ex)) { throw new BusinessException("Tên sản phẩm đã tồn tại.","Name"); }
        catch (DbException ex) when (ex.GetType().GetProperty("Number")?.GetValue(ex) is 547)
        { throw new BusinessException("Loại bánh đã thay đổi hoặc không còn tồn tại. Hãy tải lại.","Category"); }
    }
}
