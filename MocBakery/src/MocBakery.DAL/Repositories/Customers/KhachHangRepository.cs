using System.Data;
using System.Data.Common;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Customers;
namespace MocBakery.DAL.Repositories.Customers;
public sealed class KhachHangRepository(DbConnectionFactory db) : ICustomerRepository
{
    public IReadOnlyList<KhachHang> Search(string keyword)
    {
        using var c = db.Open();
        using var q = Db.Command(c,"SELECT MaKhachHang,HoTen,SoDienThoai,DiaChi,GhiChu FROM dbo.KhachHang WHERE HoTen LIKE @key ESCAPE '~' OR SoDienThoai LIKE @key ESCAPE '~' ORDER BY MaKhachHang DESC");
        string escaped = keyword.Replace("~","~~").Replace("%","~%").Replace("_","~_").Replace("[","~[");
        q.Param("@key","%" + escaped + "%",DbType.String,500);
        using var r = q.ExecuteReader(); List<KhachHang> rows = [];
        while(r.Read()) rows.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),r.IsDBNull(4)?null:r.GetString(4)));
        return rows;
    }
    public int Save(KhachHang a)
    {
        using var c = db.Open();
        string sql = a.MaKhachHang == 0
            ? "INSERT dbo.KhachHang(HoTen,SoDienThoai,DiaChi,GhiChu) OUTPUT INSERTED.MaKhachHang VALUES(@name,@phone,@address,@note)"
            : "UPDATE dbo.KhachHang SET HoTen=@name,SoDienThoai=@phone,DiaChi=@address,GhiChu=@note OUTPUT INSERTED.MaKhachHang WHERE MaKhachHang=@id";
        using var q = Db.Command(c,sql);
        q.Param("@name",a.HoTen,DbType.String,100); q.Param("@phone",a.SoDienThoai,DbType.AnsiString,15);
        q.Param("@address",a.DiaChi,DbType.String,255); q.Param("@note",a.GhiChu,DbType.String,255);
        if(a.MaKhachHang != 0) q.Param("@id",a.MaKhachHang,DbType.Int32);
        try
        {
            object? id = q.ExecuteScalar();
            if(id is null) throw new BusinessException("Khách hàng không còn tồn tại. Hãy tải lại.");
            return Convert.ToInt32(id);
        }
        catch(DbException ex) when(Db.IsDuplicate(ex)) { throw new BusinessException("Số điện thoại đã tồn tại. Sử dụng khách hàng hiện có.", "Phone"); }
    }
    public IReadOnlyList<InvoiceSummary> History(int customerId)
    {
        using var c = db.Open();
        using var q = Db.Command(c,"SELECT MaHoaDon,NgayLap,TongTien,TienCocTru,SoTienThu FROM dbo.HoaDon WHERE MaKhachHang=@id ORDER BY NgayLap DESC,MaHoaDon DESC");
        q.Param("@id",customerId,DbType.Int32); using var r = q.ExecuteReader();
        List<InvoiceSummary> rows = [];
        while(r.Read()) rows.Add(new(r.GetInt32(0),r.GetDateTime(1),r.GetDecimal(2),r.GetDecimal(3),r.GetDecimal(4)));
        return rows;
    }
}
