using System.Data;
using System.Data.Common;
using MocBakery.DAL.Repositories.Inventory;
using MocBakery.Models;
using MocBakery.Models.DTOs.Orders;
using MocBakery.Models.WebCart;
using MocBakery.Models.WebOrders;
namespace MocBakery.DAL.Repositories.Orders;

public sealed class WebOrderRepository(DbConnectionFactory db) : IWebOrderRepository
{
    public int Submit(WebPurchaseRequest request)
    {
        using var c=db.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        // Serialize submissions per customer: a concurrent retry cannot insert a second order.
        using(var account=Db.Command(c,"SELECT MaKhachHang FROM dbo.TaiKhoanKhachHang WITH(UPDLOCK,HOLDLOCK) WHERE MaKhachHang=@customer AND TrangThai=1",tx))
        {account.Param("@customer",request.CustomerId,DbType.Int32);if(account.ExecuteScalar() is null)throw new BusinessException("Tài khoản khách không còn hoạt động.");}
        using(var replay=Db.Command(c,"SELECT MaDon FROM dbo.DonDatBanh WHERE MaKhachHang=@customer AND WebRequestKey=@key",tx))
        {
            replay.Param("@customer",request.CustomerId,DbType.Int32);replay.Param("@key",request.RequestKey,DbType.Guid);
            if(replay.ExecuteScalar() is int existing){tx.Commit();return existing;}
        }
        DateTime now;using(var clock=Db.Command(c,"SELECT SYSDATETIME()",tx))now=(DateTime)clock.ExecuteScalar()!;
        if(request.Pickup is null||request.Pickup<=now)throw new BusinessException("Ngày giờ nhận phải sau thời điểm đặt bánh.");
        var lines=new List<OrderLine>();
        foreach(var item in request.Items.OrderBy(x=>x.ProductId))
        {
            string name;decimal price;
            using(var product=Db.Command(c,"""
                SELECT p.TenSanPham,p.GiaBan FROM dbo.SanPham p WITH(UPDLOCK,HOLDLOCK)
                JOIN dbo.LoaiBanh cat WITH(HOLDLOCK) ON cat.MaLoai=p.MaLoai
                WHERE p.MaSanPham=@id AND p.DangKinhDoanh=1 AND cat.DangSuDung=1
                """,tx))
            {
                product.Param("@id",item.ProductId,DbType.Int32);using var r=product.ExecuteReader();
                if(!r.Read())throw new BusinessException("Có bánh đã ngừng bán. Vui lòng kiểm tra lại giỏ.");
                name=r.GetString(0);price=r.GetDecimal(1);
            }
            if(request.ConfirmedLines?.SingleOrDefault(line=>line.ProductId==item.ProductId)?.UnitPrice!=price)
                throw new BusinessException("Giá bánh vừa thay đổi. Vui lòng xem lại và xác nhận mức giá mới.");
            using(var stock=Db.Command(c,$"SELECT COALESCE(SUM(CONVERT(bigint,l.SoLuongCon)),0) FROM dbo.LoBanh l WITH(UPDLOCK,HOLDLOCK) WHERE l.MaSanPham=@id AND {LoBanhRepository.SellableLotCondition}",tx))
            {
                stock.Param("@id",item.ProductId,DbType.Int32);stock.Param("@day",now.Date,DbType.Date);
                if(Convert.ToInt64(stock.ExecuteScalar())<item.Quantity)throw new BusinessException("Bánh không còn đủ số lượng. Vui lòng kiểm tra lại giỏ.");
            }
            lines.Add(new(item.ProductId,name,item.Quantity,price));
        }
        decimal total=lines.Sum(x=>x.Total);
        if(total!=request.ConfirmedTotal)throw new BusinessException("Giá bánh vừa thay đổi. Vui lòng xem lại và xác nhận mức giá mới.");
        using var order=Db.Command(c,"""
            INSERT dbo.DonDatBanh(MaKhachHang,MaTaiKhoan,NgayDat,NgayNhan,YeuCau,TongTien,TienCoc,WebRequestKey)
            OUTPUT INSERTED.MaDon VALUES(@customer,NULL,@now,@pickup,@note,@total,0,@key)
            """,tx);
        order.Param("@customer",request.CustomerId,DbType.Int32);order.Param("@now",now,DbType.DateTime2);
        order.Param("@pickup",request.Pickup,DbType.DateTime2);order.Param("@note",request.Note,DbType.String,500);
        order.Param("@total",total,DbType.Decimal);order.Param("@key",request.RequestKey,DbType.Guid);
        int id=Convert.ToInt32(order.ExecuteScalar());
        foreach(var line in lines)
        {
            using var q=Db.Command(c,"INSERT dbo.ChiTietDonDat(MaDon,MaSanPham,SoLuong,DonGia) VALUES(@order,@product,@quantity,@price)",tx);
            q.Param("@order",id,DbType.Int32);q.Param("@product",line.ProductId,DbType.Int32);q.Param("@quantity",line.Quantity,DbType.Int32);q.Param("@price",line.Price,DbType.Decimal);q.ExecuteNonQuery();
        }
        tx.Commit();return id;
    }

    public IReadOnlyList<CustomerOrder> List(int customerId)
    {
        using var c=db.Open();using var q=Db.Command(c,"SELECT MaDon,NgayDat,NgayNhan,TongTien,TrangThai,YeuCau,LyDoHuy FROM dbo.DonDatBanh WHERE MaKhachHang=@customer ORDER BY NgayDat DESC,MaDon DESC");
        q.Param("@customer",customerId,DbType.Int32);using var r=q.ExecuteReader();var result=new List<CustomerOrder>();while(r.Read())result.Add(Read(r));return result;
    }

    public CustomerOrderDetail? Find(int customerId,int orderId)
    {
        using var c=db.Open();using var q=Db.Command(c,"""
            SELECT MaDon,NgayDat,NgayNhan,TongTien,TrangThai,YeuCau,LyDoHuy FROM dbo.DonDatBanh
            WHERE MaKhachHang=@customer AND MaDon=@id;
            SELECT d.MaSanPham,p.TenSanPham,d.SoLuong,d.DonGia FROM dbo.ChiTietDonDat d
            JOIN dbo.DonDatBanh o ON o.MaDon=d.MaDon JOIN dbo.SanPham p ON p.MaSanPham=d.MaSanPham
            WHERE o.MaKhachHang=@customer AND o.MaDon=@id ORDER BY d.MaSanPham;
            """);
        q.Param("@customer",customerId,DbType.Int32);q.Param("@id",orderId,DbType.Int32);using var r=q.ExecuteReader();
        if(!r.Read())return null;var summary=Read(r);r.NextResult();var lines=new List<OrderLine>();
        while(r.Read())lines.Add(new(r.GetInt32(0),r.GetString(1),r.GetInt32(2),r.GetDecimal(3)));return new(summary,lines);
    }
    private static CustomerOrder Read(DbDataReader r)=>new(r.GetInt32(0),r.GetDateTime(1),r.GetDateTime(2),r.GetDecimal(3),r.GetString(4),r.IsDBNull(5)?null:r.GetString(5),r.IsDBNull(6)?null:r.GetString(6));
}
