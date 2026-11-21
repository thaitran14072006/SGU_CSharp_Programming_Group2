using System.Data;
using System.Data.Common;
using MocBakery.Models;
using MocBakery.Models.WebCart;
namespace MocBakery.DAL.Repositories.Sales;

public sealed class CustomerCartRepository(DbConnectionFactory db) : ICustomerCartRepository
{
    // Serialize cart operations for the same owner, even before their first cart exists.
    private static void LockOwner(DbConnection c, DbTransaction tx, int id)
    {
        using var q=Db.Command(c,"SELECT MaKhachHang FROM dbo.TaiKhoanKhachHang WITH(UPDLOCK,HOLDLOCK) WHERE MaKhachHang=@id AND TrangThai=1",tx);
        q.Param("@id",id,DbType.Int32);
        if(q.ExecuteScalar() is null)throw new BusinessException("Tài khoản khách không còn hoạt động.");
    }
    private static SavedCart Load(DbConnection c,DbTransaction tx,int id)
    {
        using var q=Db.Command(c,"SELECT PhienBan FROM dbo.GioHangKhachHang WHERE MaKhachHang=@id; SELECT MaSanPham,SoLuong FROM dbo.ChiTietGioHang WHERE MaKhachHang=@id ORDER BY MaSanPham",tx);
        q.Param("@id",id,DbType.Int32);using var r=q.ExecuteReader();long version=r.Read()?r.GetInt64(0):0;
        r.NextResult();List<CartItem> items=[];while(r.Read())items.Add(new(r.GetInt32(0),r.GetInt32(1)));
        return new(version,items);
    }
    public SavedCart Read(int customerId)
    {
        using var c=db.Open();using var tx=c.BeginTransaction();LockOwner(c,tx,customerId);
        var state=Load(c,tx,customerId);tx.Commit();return state;
    }
    public SavedCart Update(int customerId,Func<IReadOnlyList<CartItem>,IReadOnlyList<CartItem>> change)
    {
        using var c=db.Open();using var tx=c.BeginTransaction();LockOwner(c,tx,customerId);
        var before=Load(c,tx,customerId);var items=change(before.Items);
        if(items.Count>50||items.Any(x=>x.ProductId<=0||x.Quantity is <1 or >1000)||items.Select(x=>x.ProductId).Distinct().Count()!=items.Count)
            throw new BusinessException("Giỏ hàng không hợp lệ.");
        Save(c,tx,customerId,before.Version+1,items);tx.Commit();return new(before.Version+1,items);
    }
    public bool ClearIfVersion(int customerId,long version)
    {
        using var c=db.Open();using var tx=c.BeginTransaction();LockOwner(c,tx,customerId);
        if(Load(c,tx,customerId).Version!=version){tx.Commit();return false;}
        Save(c,tx,customerId,version+1,[]);tx.Commit();return true;
    }
    private static void Save(DbConnection c,DbTransaction tx,int id,long version,IReadOnlyList<CartItem> items)
    {
        using(var q=Db.Command(c,"IF NOT EXISTS(SELECT 1 FROM dbo.GioHangKhachHang WHERE MaKhachHang=@id) INSERT dbo.GioHangKhachHang(MaKhachHang) VALUES(@id); UPDATE dbo.GioHangKhachHang SET PhienBan=@version,CapNhatLuc=SYSDATETIME() WHERE MaKhachHang=@id; DELETE dbo.ChiTietGioHang WHERE MaKhachHang=@id",tx))
        {q.Param("@id",id,DbType.Int32);q.Param("@version",version,DbType.Int64);q.ExecuteNonQuery();}
        foreach(var item in items.OrderBy(x=>x.ProductId))
        {using var q=Db.Command(c,"INSERT dbo.ChiTietGioHang(MaKhachHang,MaSanPham,SoLuong) VALUES(@id,@product,@quantity)",tx);q.Param("@id",id,DbType.Int32);q.Param("@product",item.ProductId,DbType.Int32);q.Param("@quantity",item.Quantity,DbType.Int32);q.ExecuteNonQuery();}
    }
}
