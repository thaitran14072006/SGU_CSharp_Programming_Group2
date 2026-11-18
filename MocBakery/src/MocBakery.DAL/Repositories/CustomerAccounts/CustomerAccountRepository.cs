using System.Data;
using System.Data.Common;
using MocBakery.Models;
using MocBakery.Models.CustomerAccounts;

namespace MocBakery.DAL.Repositories.CustomerAccounts;

public sealed class CustomerAccountRepository(DbConnectionFactory db) : ICustomerAccountRepository
{
    private const string Columns = "MaTaiKhoanKhach,MaKhachHang,TenDangNhap,MatKhauHash,TrangThai,SecurityStamp";
    private static CustomerAccount Read(DbDataReader r) => new(r.GetInt32(0), r.GetInt32(1),
        r.GetString(2), r.GetString(3), r.GetBoolean(4), r.GetGuid(5));

    public CustomerAccount? FindByUsername(string username)
    {
        using var c = db.Open();
        using var q = Db.Command(c, $"SELECT {Columns} FROM dbo.TaiKhoanKhachHang WHERE TenDangNhap=@u");
        q.Param("@u", username, DbType.String, 50);
        using var r = q.ExecuteReader();
        return r.Read() ? Read(r) : null;
    }

    public CustomerAccount? FindById(int id)
    {
        using var c = db.Open();
        using var q = Db.Command(c, $"SELECT {Columns} FROM dbo.TaiKhoanKhachHang WHERE MaTaiKhoanKhach=@id");
        q.Param("@id", id, DbType.Int32);
        using var r = q.ExecuteReader();
        return r.Read() ? Read(r) : null;
    }

    public int Register(string username, string passwordHash, CustomerProfile profile)
    {
        using var c = db.Open();
        using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        using (var duplicate = Db.Command(c, "SELECT COUNT(*) FROM dbo.TaiKhoanKhachHang WITH(UPDLOCK,HOLDLOCK) WHERE TenDangNhap=@u", tx))
        {
            duplicate.Param("@u", username, DbType.String, 50);
            if (Convert.ToInt32(duplicate.ExecuteScalar()) > 0)
                throw new BusinessException("Tên đăng nhập đã tồn tại.", "Username");
        }
        // Never attach a public registration to an existing customer just by knowing their phone.
        using (var phone = Db.Command(c, "SELECT COUNT(*) FROM dbo.KhachHang WITH(UPDLOCK,HOLDLOCK) WHERE SoDienThoai=@phone", tx))
        {
            phone.Param("@phone", profile.Phone, DbType.AnsiString, 15);
            if (Convert.ToInt32(phone.ExecuteScalar()) > 0)
                throw new BusinessException("Số điện thoại đã có hồ sơ. Liên hệ cửa hàng để xác minh; không thể tự liên kết hồ sơ cũ.", "Phone");
        }
        int customerId;
        using (var customer = Db.Command(c, "INSERT dbo.KhachHang(HoTen,SoDienThoai,DiaChi) OUTPUT INSERTED.MaKhachHang VALUES(@name,@phone,@address)", tx))
        {
            ProfileParams(customer, profile);
            customerId = Convert.ToInt32(customer.ExecuteScalar());
        }
        using var account = Db.Command(c, "INSERT dbo.TaiKhoanKhachHang(MaKhachHang,TenDangNhap,MatKhauHash) OUTPUT INSERTED.MaTaiKhoanKhach VALUES(@customer,@u,@hash)", tx);
        account.Param("@customer", customerId, DbType.Int32);
        account.Param("@u", username, DbType.String, 50);
        account.Param("@hash", passwordHash, DbType.String, 255);
        int id = Convert.ToInt32(account.ExecuteScalar());
        tx.Commit();
        return id;
    }

    private const string IdentityWhere = "a.MaTaiKhoanKhach=@id AND a.MaKhachHang=@customer AND a.SecurityStamp=@stamp AND a.TrangThai=1";
    private static void IdentityParams(DbCommand q, CustomerIdentity identity)
    {
        q.Param("@id", identity.AccountId, DbType.Int32);
        q.Param("@customer", identity.CustomerId, DbType.Int32);
        q.Param("@stamp", identity.SecurityStamp, DbType.Guid);
    }
    private static void ProfileParams(DbCommand q, CustomerProfile profile)
    {
        q.Param("@name", profile.FullName, DbType.String, 100);
        q.Param("@phone", profile.Phone, DbType.AnsiString, 15);
        q.Param("@address", profile.Address, DbType.String, 255);
    }

    public CustomerProfile? ReadProfile(CustomerIdentity identity)
    {
        using var c = db.Open();
        using var q = Db.Command(c, $"SELECT k.HoTen,k.SoDienThoai,k.DiaChi FROM dbo.KhachHang k JOIN dbo.TaiKhoanKhachHang a ON a.MaKhachHang=k.MaKhachHang WHERE {IdentityWhere}");
        IdentityParams(q, identity);
        using var r = q.ExecuteReader();
        return r.Read() ? new(r.GetString(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2)) : null;
    }

    public bool UpdateProfile(CustomerIdentity identity, CustomerProfile profile)
    {
        using var c = db.Open();
        using var q = Db.Command(c, $"UPDATE k SET HoTen=@name,SoDienThoai=@phone,DiaChi=@address FROM dbo.KhachHang k JOIN dbo.TaiKhoanKhachHang a ON a.MaKhachHang=k.MaKhachHang WHERE {IdentityWhere}");
        IdentityParams(q, identity);
        ProfileParams(q, profile);
        try { return q.ExecuteNonQuery() == 1; }
        catch (DbException ex) when (Db.IsDuplicate(ex))
        { throw new BusinessException("Số điện thoại đã có hồ sơ khác.", "Phone"); }
    }

    public bool ChangePassword(CustomerIdentity identity, string expectedHash, string newHash)
    {
        using var c = db.Open();
        using var q = Db.Command(c, $"UPDATE a SET MatKhauHash=@next,SecurityStamp=NEWID() FROM dbo.TaiKhoanKhachHang a WHERE {IdentityWhere} AND MatKhauHash=@expected");
        IdentityParams(q, identity);
        q.Param("@next", newHash, DbType.String, 255);
        q.Param("@expected", expectedHash, DbType.String, 255);
        return q.ExecuteNonQuery() == 1;
    }
}
