using System.Data;
using System.Data.Common;
using MocBakery.Models;
using MocBakery.Models.Entities;
namespace MocBakery.DAL.Repositories.Catalog;
public sealed class LoaiBanhRepository(DbConnectionFactory db) : ILoaiBanhRepository
{
    public IReadOnlyList<LoaiBanh> List()
    {
        using var c = db.Open();
        using var q = Db.Command(c, "SELECT MaLoai,TenLoai,MoTa,DangSuDung FROM dbo.LoaiBanh ORDER BY TenLoai,MaLoai");
        using var r = q.ExecuteReader(); List<LoaiBanh> rows = [];
        while (r.Read()) rows.Add(new(r.GetInt32(0),r.GetString(1),r.IsDBNull(2) ? null : r.GetString(2),r.GetBoolean(3)));
        return rows;
    }
    public int Save(LoaiBanh input)
    {
        using var c = db.Open();
        using var q = Db.Command(c, input.MaLoai == 0
            ? "INSERT dbo.LoaiBanh(TenLoai,MoTa,DangSuDung) OUTPUT INSERTED.MaLoai VALUES(@name,@note,@active)"
            : "UPDATE dbo.LoaiBanh SET TenLoai=@name,MoTa=@note,DangSuDung=@active OUTPUT INSERTED.MaLoai WHERE MaLoai=@id");
        q.Param("@active",input.DangSuDung,DbType.Boolean);
        q.Param("@name",input.TenLoai,DbType.String,100); q.Param("@note",input.MoTa,DbType.String,255);
        if (input.MaLoai != 0) q.Param("@id",input.MaLoai,DbType.Int32);
        try { return Convert.ToInt32(q.ExecuteScalar() ?? throw new BusinessException("Loại bánh không còn tồn tại. Hãy tải lại.")); }
        catch (DbException ex) when (Db.IsDuplicate(ex)) { throw new BusinessException("Tên loại bánh đã tồn tại.","Name"); }
    }
    public void Delete(int id)
    {
        using var c = db.Open(); using var q = Db.Command(c,"DELETE dbo.LoaiBanh WHERE MaLoai=@id");
        q.Param("@id",id,DbType.Int32);
        try { if (q.ExecuteNonQuery() != 1) throw new BusinessException("Loại bánh không còn tồn tại. Hãy tải lại."); }
        catch (DbException ex) when (ex.GetType().GetProperty("Number")?.GetValue(ex) is 547)
        { throw new BusinessException("Loại bánh đang được sản phẩm sử dụng, không thể xóa."); }
    }
}
