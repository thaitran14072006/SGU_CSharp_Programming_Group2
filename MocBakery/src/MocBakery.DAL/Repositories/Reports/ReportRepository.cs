using System.Data;
using MocBakery.Models.DTOs.Reports;
namespace MocBakery.DAL.Repositories.Reports;
public sealed class ReportRepository(DbConnectionFactory db):IReportRepository
{
 public IReadOnlyList<RevenueDay> Revenue(DateTime from,DateTime to)
 {
  using var c=db.Open();using var cmd=Db.Command(c,"SELECT CONVERT(date,NgayLap),COUNT(*),SUM(TongTien) FROM dbo.HoaDon WHERE NgayLap>=@from AND NgayLap<@to GROUP BY CONVERT(date,NgayLap) ORDER BY CONVERT(date,NgayLap)");Range(cmd,from,to);using var r=cmd.ExecuteReader();var list=new List<RevenueDay>();while(r.Read())list.Add(new(r.GetDateTime(0),r.GetInt32(1),r.GetDecimal(2)));return list;
 }
 public IReadOnlyList<ProductSales> Bestsellers(DateTime from,DateTime to)
 {
  using var c=db.Open();using var cmd=Db.Command(c,"SELECT p.MaSanPham,p.TenSanPham,SUM(CONVERT(bigint,d.SoLuong)),SUM(d.SoLuong*d.DonGia) FROM dbo.HoaDon h JOIN dbo.ChiTietHoaDon d ON d.MaHoaDon=h.MaHoaDon JOIN dbo.LoBanh l ON l.MaLo=d.MaLo JOIN dbo.SanPham p ON p.MaSanPham=l.MaSanPham WHERE h.NgayLap>=@from AND h.NgayLap<@to GROUP BY p.MaSanPham,p.TenSanPham ORDER BY SUM(d.SoLuong) DESC");Range(cmd,from,to);using var r=cmd.ExecuteReader();var list=new List<ProductSales>();while(r.Read())list.Add(new(r.GetInt32(0),r.GetString(1),r.GetInt64(2),r.GetDecimal(3)));return list;
 }
 public Dashboard Overview(DateTime from,DateTime to)
 {
  using var c=db.Open();using var cmd=Db.Command(c,"SELECT COALESCE(SUM(TongTien),0),COUNT(*) FROM dbo.HoaDon WHERE NgayLap>=@from AND NgayLap<@to");Range(cmd,from,to);decimal revenue;int count;using(var r=cmd.ExecuteReader()){r.Read();revenue=r.GetDecimal(0);count=r.GetInt32(1);}
  using var pending=Db.Command(c,"SELECT COUNT(*) FROM dbo.DonDatBanh WHERE TrangThai IN(N'MoiTiepNhan',N'DangChuanBi')");int orders=Convert.ToInt32(pending.ExecuteScalar());
  using var stock=Db.Command(c,"SELECT COALESCE(SUM(CONVERT(bigint,l.SoLuongCon)),0) FROM dbo.LoBanh l JOIN dbo.SanPham p ON p.MaSanPham=l.MaSanPham WHERE p.DangKinhDoanh=1 AND l.SoLuongCon>0 AND l.TinhTrang=N'DangBan' AND l.HanSuDung>=CONVERT(date,SYSDATETIME())");long quantity=Convert.ToInt64(stock.ExecuteScalar());return new(revenue,count,orders,quantity);
 }
 public IReadOnlyList<LowStockItem> LowStock(int threshold)
 {
  using var c=db.Open();using var cmd=Db.Command(c,"""
    SELECT p.MaSanPham,p.TenSanPham,COALESCE(SUM(CONVERT(bigint,l.SoLuongCon)),0)
    FROM dbo.SanPham p LEFT JOIN dbo.LoBanh l ON p.MaSanPham=l.MaSanPham
      AND l.TinhTrang=N'DangBan' AND l.HanSuDung>=CONVERT(date,SYSDATETIME())
    WHERE p.DangKinhDoanh=1
    GROUP BY p.MaSanPham,p.TenSanPham
    HAVING COALESCE(SUM(CONVERT(bigint,l.SoLuongCon)),0)<@threshold
    ORDER BY 3,p.TenSanPham
    """);cmd.Param("@threshold",threshold,DbType.Int32);
  using var r=cmd.ExecuteReader();var result=new List<LowStockItem>();
  while(r.Read())result.Add(new(r.GetInt32(0),r.GetString(1),r.GetInt64(2)));
  return result;
 }
 public IReadOnlyList<UpcomingOrder> Upcoming(DateTime day)
 {
  using var c=db.Open();using var cmd=Db.Command(c,"""
    SELECT d.MaDon,k.HoTen,d.NgayNhan,d.TrangThai
    FROM dbo.DonDatBanh d JOIN dbo.KhachHang k ON k.MaKhachHang=d.MaKhachHang
    WHERE d.TrangThai IN(N'MoiTiepNhan',N'DangChuanBi') AND d.NgayNhan<@end
    ORDER BY d.NgayNhan,d.MaDon
    """);cmd.Param("@end",day.Date.AddDays(1),DbType.DateTime2);
  using var r=cmd.ExecuteReader();var result=new List<UpcomingOrder>();
  while(r.Read())result.Add(new(r.GetInt32(0),r.GetString(1),r.GetDateTime(2),r.GetString(3)));
  return result;
 }
 private static void Range(System.Data.Common.DbCommand cmd,DateTime from,DateTime to){cmd.Param("@from",from,DbType.DateTime2);cmd.Param("@to",to,DbType.DateTime2);}
}
