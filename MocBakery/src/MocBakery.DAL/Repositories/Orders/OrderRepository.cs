using System.Data;
using System.Data.Common;
using MocBakery.DAL.Repositories.Sales;
using MocBakery.Models;
using MocBakery.Models.DTOs.Orders;
using MocBakery.Models.DTOs.Sales;
using MocBakery.Models.Entities;
namespace MocBakery.DAL.Repositories.Orders;
public sealed class OrderRepository(DbConnectionFactory db):IOrderRepository
{
 public IReadOnlyList<OrderSummary> List()
 {
  using var c=db.Open();using var metadata=Db.Command(c,"SELECT COL_LENGTH('dbo.DonDatBanh','EmailNhan')");
  string contactColumns=metadata.ExecuteScalar() is not null and not DBNull?"d.NguoiNhan,d.DienThoaiNhan,d.EmailNhan":"CAST(NULL AS NVARCHAR(100)),CAST(NULL AS VARCHAR(10)),CAST(NULL AS NVARCHAR(254))";
  using var cmd=Db.Command(c,$"SELECT d.MaDon,d.MaKhachHang,k.HoTen,d.MaTaiKhoan,d.NgayDat,d.NgayNhan,d.TongTien,d.TienCoc,d.TrangThai,d.YeuCau,d.LyDoHuy,{contactColumns} FROM dbo.DonDatBanh d JOIN dbo.KhachHang k ON k.MaKhachHang=d.MaKhachHang ORDER BY d.NgayNhan,d.MaDon");using var r=cmd.ExecuteReader();var result=new List<OrderSummary>();
  while(r.Read())result.Add(new(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.IsDBNull(3)?null:r.GetInt32(3),r.GetDateTime(4),r.GetDateTime(5),r.GetDecimal(6),r.GetDecimal(7),r.GetString(8),r.IsDBNull(9)?null:r.GetString(9),r.IsDBNull(10)?null:r.GetString(10))
  {Contact=r.IsDBNull(11)||r.IsDBNull(12)||r.IsDBNull(13)?null:new(r.GetString(11),r.GetString(12),r.GetString(13))});return result;
 }
 public IReadOnlyList<OrderLine> Lines(int id)
 {
  using var c=db.Open();using var cmd=Db.Command(c,"SELECT d.MaSanPham,p.TenSanPham,d.SoLuong,d.DonGia FROM dbo.ChiTietDonDat d JOIN dbo.SanPham p ON p.MaSanPham=d.MaSanPham WHERE d.MaDon=@id ORDER BY d.MaSanPham");cmd.Param("@id",id,DbType.Int32);using var r=cmd.ExecuteReader();var result=new List<OrderLine>();while(r.Read())result.Add(new(r.GetInt32(0),r.GetString(1),r.GetInt32(2),r.GetDecimal(3)));return result;
 }
 public int Create(int customerId,int accountId,DateTime pickup,decimal deposit,string? note,IReadOnlyList<OrderLine> lines)
 {
  using var c=db.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
  DateTime now=Clock(c,tx);if(pickup<=now)throw new BusinessException("Ngày nhận phải sau thời điểm đặt.");
  VerifyPrices(c,tx,lines);
  decimal total=lines.Sum(x=>x.Total);if(deposit>total)throw new BusinessException("Tiền cọc vượt tổng tiền.");
  using var cmd=Db.Command(c,"INSERT dbo.DonDatBanh(MaKhachHang,MaTaiKhoan,NgayDat,NgayNhan,YeuCau,TongTien,TienCoc) OUTPUT INSERTED.MaDon VALUES(@customer,@actor,@now,@pickup,@note,@total,@deposit)",tx);
  cmd.Param("@customer",customerId,DbType.Int32);cmd.Param("@actor",accountId,DbType.Int32);cmd.Param("@now",now,DbType.DateTime2);cmd.Param("@pickup",pickup,DbType.DateTime2);cmd.Param("@note",note,DbType.String,500);cmd.Param("@total",total,DbType.Decimal);cmd.Param("@deposit",deposit,DbType.Decimal);
  int id=Convert.ToInt32(cmd.ExecuteScalar());InsertLines(c,tx,id,lines);tx.Commit();return id;
 }
 public void Update(int id,int customerId,DateTime pickup,decimal deposit,string? note,IReadOnlyList<OrderLine> lines)
 {
  using var c=db.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
  var (state,placed)=LockOrder(c,tx,id);if(state is not ("MoiTiepNhan" or "DangChuanBi"))throw new BusinessException("Đơn đã giao/hủy không thể sửa.");
  if(pickup<=placed)throw new BusinessException("Ngày nhận phải sau ngày đặt.");
  VerifyPrices(c,tx,lines);
  decimal total=lines.Sum(x=>x.Total);if(deposit>total)throw new BusinessException("Tiền cọc vượt tổng tiền.");
  using(var cmd=Db.Command(c,"UPDATE dbo.DonDatBanh SET MaKhachHang=@customer,NgayNhan=@pickup,YeuCau=@note,TongTien=@total,TienCoc=@deposit WHERE MaDon=@id",tx))
  {cmd.Param("@customer",customerId,DbType.Int32);cmd.Param("@pickup",pickup,DbType.DateTime2);cmd.Param("@note",note,DbType.String,500);cmd.Param("@total",total,DbType.Decimal);cmd.Param("@deposit",deposit,DbType.Decimal);cmd.Param("@id",id,DbType.Int32);cmd.ExecuteNonQuery();}
  using(var cmd=Db.Command(c,"DELETE dbo.ChiTietDonDat WHERE MaDon=@id",tx)){cmd.Param("@id",id,DbType.Int32);cmd.ExecuteNonQuery();}
  InsertLines(c,tx,id,lines);tx.Commit();
 }
 public void Prepare(int id,int accountId){using var c=db.Open();using var cmd=Db.Command(c,"UPDATE dbo.DonDatBanh SET TrangThai=N'DangChuanBi',MaTaiKhoan=COALESCE(MaTaiKhoan,@actor) WHERE MaDon=@id AND TrangThai=N'MoiTiepNhan'");cmd.Param("@id",id,DbType.Int32);cmd.Param("@actor",accountId,DbType.Int32);if(cmd.ExecuteNonQuery()!=1)throw new BusinessException("Chỉ đơn mới tiếp nhận được chuyển sang chuẩn bị.");}
 public void Cancel(int id,string reason)
 {
  using var c=db.Open();using var cmd=Db.Command(c,"UPDATE dbo.DonDatBanh SET TrangThai=N'DaHuy',LyDoHuy=@reason WHERE MaDon=@id AND TrangThai IN(N'MoiTiepNhan',N'DangChuanBi')");cmd.Param("@reason",reason,DbType.String,255);cmd.Param("@id",id,DbType.Int32);if(cmd.ExecuteNonQuery()!=1)throw new BusinessException("Đơn không thể hủy ở trạng thái hiện tại.");
 }
 public int Deliver(int id,int actor,Func<IEnumerable<LoBanh>,int,IReadOnlyList<LotAllocation>> allocate)
 {
  using var c=db.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
  var (state,_)=LockOrder(c,tx,id);
  if(state!="DangChuanBi")throw new BusinessException("Chỉ giao được đơn đang chuẩn bị.");
  int customer;decimal deposit;
  using(var cmd=Db.Command(c,"SELECT MaKhachHang,TienCoc FROM dbo.DonDatBanh WHERE MaDon=@id",tx))
  {cmd.Param("@id",id,DbType.Int32);using var r=cmd.ExecuteReader();if(!r.Read())throw new BusinessException("Không tìm thấy đơn.");customer=r.GetInt32(0);deposit=r.GetDecimal(1);}
  var rows=new List<CartItem>();
  using(var cmd=Db.Command(c,"SELECT MaSanPham,SoLuong,DonGia FROM dbo.ChiTietDonDat WHERE MaDon=@id ORDER BY MaSanPham",tx))
  {cmd.Param("@id",id,DbType.Int32);using var r=cmd.ExecuteReader();while(r.Read())rows.Add(new(r.GetInt32(0),r.GetInt32(1),r.GetDecimal(2)));}
  if(rows.Count==0)throw new BusinessException("Đơn không có sản phẩm.");
  int invoice=SalesRepository.WriteInvoice(tx,actor,customer,id,deposit,rows,allocate);
  using(var cmd=Db.Command(c,"UPDATE dbo.DonDatBanh SET TrangThai=N'DaGiao' WHERE MaDon=@id AND TrangThai=N'DangChuanBi'",tx))
  {cmd.Param("@id",id,DbType.Int32);if(cmd.ExecuteNonQuery()!=1)throw new BusinessException("Trạng thái đơn đã thay đổi.");}
  tx.Commit();return invoice;
 }
 private static (string state,DateTime placed) LockOrder(DbConnection c,DbTransaction t,int id)
 {
  using var cmd=Db.Command(c,"SELECT TrangThai,NgayDat FROM dbo.DonDatBanh WITH(UPDLOCK,HOLDLOCK) WHERE MaDon=@id",t);cmd.Param("@id",id,DbType.Int32);using var r=cmd.ExecuteReader();if(!r.Read())throw new BusinessException("Không tìm thấy đơn.");return(r.GetString(0),r.GetDateTime(1));
 }
 private static DateTime Clock(DbConnection c,DbTransaction t){using var cmd=Db.Command(c,"SELECT SYSDATETIME()",t);return (DateTime)cmd.ExecuteScalar()!;}
 private static void VerifyPrices(DbConnection c,DbTransaction t,IReadOnlyList<OrderLine> lines)
 {
  foreach(var line in lines.OrderBy(x=>x.ProductId))
  {using var cmd=Db.Command(c,"SELECT GiaBan FROM dbo.SanPham WITH(UPDLOCK,HOLDLOCK) WHERE MaSanPham=@id AND DangKinhDoanh=1",t);cmd.Param("@id",line.ProductId,DbType.Int32);if(cmd.ExecuteScalar() is not decimal price||price!=line.Price)throw new BusinessException("Giá hoặc trạng thái sản phẩm đã thay đổi.");}
 }
 private static void InsertLines(DbConnection c,DbTransaction t,int id,IReadOnlyList<OrderLine> lines)
 {
  foreach(var line in lines){using var cmd=Db.Command(c,"INSERT dbo.ChiTietDonDat(MaDon,MaSanPham,SoLuong,DonGia) VALUES(@order,@product,@quantity,@price)",t);cmd.Param("@order",id,DbType.Int32);cmd.Param("@product",line.ProductId,DbType.Int32);cmd.Param("@quantity",line.Quantity,DbType.Int32);cmd.Param("@price",line.Price,DbType.Decimal);cmd.ExecuteNonQuery();}
 }
}
