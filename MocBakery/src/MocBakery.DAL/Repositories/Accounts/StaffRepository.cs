using System.Data;
using System.Data.Common;
using MocBakery.Models;
using MocBakery.Models.Entities;
namespace MocBakery.DAL.Repositories.Accounts;

public sealed class StaffRepository(DbConnectionFactory db) : IStaffRepository
{
    public IReadOnlyList<NhanVien> List()
    {
        using var c=db.Open();using var q=Db.Command(c,"SELECT MaNhanVien,HoTen,SoDienThoai,ChucVu,NgayVaoLam,DangLam,MaTaiKhoan FROM dbo.NhanVien ORDER BY MaNhanVien");
        using var r=q.ExecuteReader();List<NhanVien> rows=[];
        while(r.Read()) rows.Add(new(r.GetInt32(0),r.GetString(1),r.IsDBNull(2)?null:r.GetString(2),r.GetString(3),DateOnly.FromDateTime(r.GetDateTime(4)),r.GetBoolean(5),r.IsDBNull(6)?null:r.GetInt32(6)));
        return rows;
    }

    // Use the same account -> employee lock order for all profile/account writes.
    private static List<TaiKhoan> LockAccounts(DbConnection c,DbTransaction tx,int actor)
    {
        using var q=Db.Command(c,"SELECT MaTaiKhoan,TenDangNhap,MatKhauHash,HoTen,VaiTro,TrangThai,NgayTao FROM dbo.TaiKhoan WITH(UPDLOCK,HOLDLOCK) ORDER BY MaTaiKhoan",tx);
        using var r=q.ExecuteReader();List<TaiKhoan> rows=[];
        while(r.Read())rows.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetBoolean(5),r.GetDateTime(6)));
        if(!rows.Any(a=>a.MaTaiKhoan==actor&&a.TrangThai&&a.VaiTro==Roles.ChuTiem))throw new BusinessException("Bạn không còn quyền quản lý.");
        return rows;
    }

    private static void GuardOwner(List<TaiKhoan> accounts,int actor,int id,bool active,string role)
    {
        var original=accounts.Single(a=>a.MaTaiKhoan==id);
        if((!active||role!=Roles.ChuTiem)&&(id==actor||original.TrangThai&&original.VaiTro==Roles.ChuTiem&&accounts.Count(a=>a.TrangThai&&a.VaiTro==Roles.ChuTiem)==1))
            throw new BusinessException("Không tự khóa/hạ quyền hoặc khóa quản lý hoạt động cuối cùng.","Active");
    }

    public int Save(int actorId,NhanVien a)
    {
        using var c=db.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        var accounts=LockAccounts(c,tx,actorId);int? account=null;
        if(a.MaNhanVien>0)
        {
            using var read=Db.Command(c,"SELECT MaTaiKhoan FROM dbo.NhanVien WITH(UPDLOCK,HOLDLOCK) WHERE MaNhanVien=@id",tx);read.Param("@id",a.MaNhanVien,DbType.Int32);
            object? value=read.ExecuteScalar();if(value is null)throw new BusinessException("Nhân viên không còn tồn tại.");
            account=value is DBNull?null:Convert.ToInt32(value);
        }
        if(account is int linked)
        {
            var original=accounts.Single(x=>x.MaTaiKhoan==linked);
            GuardOwner(accounts,actorId,linked,a.DangLam&&original.TrangThai,a.ChucVu);
            using var update=Db.Command(c,"UPDATE dbo.TaiKhoan SET HoTen=@name,VaiTro=@role,TrangThai=CASE WHEN @working=0 THEN 0 ELSE TrangThai END WHERE MaTaiKhoan=@id",tx);
            update.Param("@name",a.HoTen,DbType.String,100);update.Param("@role",a.ChucVu,DbType.String,20);update.Param("@working",a.DangLam,DbType.Boolean);update.Param("@id",linked,DbType.Int32);update.ExecuteNonQuery();
        }
        using var q=Db.Command(c,a.MaNhanVien==0?
            "INSERT dbo.NhanVien(HoTen,SoDienThoai,ChucVu,NgayVaoLam,DangLam) OUTPUT INSERTED.MaNhanVien VALUES(@name,@phone,@role,@date,@working)":
            "UPDATE dbo.NhanVien SET HoTen=@name,SoDienThoai=@phone,ChucVu=@role,NgayVaoLam=@date,DangLam=@working OUTPUT INSERTED.MaNhanVien WHERE MaNhanVien=@id",tx);
        q.Param("@id",a.MaNhanVien,DbType.Int32);q.Param("@name",a.HoTen,DbType.String,100);q.Param("@phone",a.SoDienThoai,DbType.AnsiString,10);
        q.Param("@role",a.ChucVu,DbType.String,20);q.Param("@date",a.NgayVaoLam.ToDateTime(TimeOnly.MinValue),DbType.Date);q.Param("@working",a.DangLam,DbType.Boolean);
        try{int id=Convert.ToInt32(q.ExecuteScalar());tx.Commit();return id;}
        catch(DbException ex) when(Db.IsDuplicate(ex)){throw new BusinessException("Điện thoại nhân viên đã tồn tại.","Phone");}
    }

    public int SaveAccount(int actorId,int employeeId,int accountId,AccountData a)
    {
        using var c=db.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        var accounts=LockAccounts(c,tx,actorId);
        using(var read=Db.Command(c,"SELECT MaTaiKhoan,DangLam FROM dbo.NhanVien WITH(UPDLOCK,HOLDLOCK) WHERE MaNhanVien=@id",tx))
        {
            read.Param("@id",employeeId,DbType.Int32);using var r=read.ExecuteReader();
            if(!r.Read()||!r.GetBoolean(1))throw new BusinessException("Chọn nhân viên đang làm.","Employee");
            int linked=r.IsDBNull(0)?0:r.GetInt32(0);
            if(linked!=accountId)throw new BusinessException("Liên kết tài khoản đã thay đổi. Hãy tải lại.","Employee");
        }
        if(accountId>0)
        {
            var original=accounts.SingleOrDefault(x=>x.MaTaiKhoan==accountId)??throw new BusinessException("Tài khoản không tồn tại.");
            if(original.TenDangNhap!=a.Username)throw new BusinessException("Không đổi tên đăng nhập.","Username");
            GuardOwner(accounts,actorId,accountId,a.Active,a.Role);
        }
        using(var q=Db.Command(c,accountId==0?
            "INSERT dbo.TaiKhoan(TenDangNhap,HoTen,VaiTro,TrangThai,MatKhauHash,BatBuocDoiMatKhau) OUTPUT INSERTED.MaTaiKhoan VALUES(@username,@name,@role,1,@hash,1)":
            "UPDATE dbo.TaiKhoan SET HoTen=@name,VaiTro=@role,TrangThai=@active OUTPUT INSERTED.MaTaiKhoan WHERE MaTaiKhoan=@id",tx))
        {
            q.Param("@id",accountId,DbType.Int32);q.Param("@username",a.Username,DbType.String,50);q.Param("@name",a.FullName,DbType.String,100);
            q.Param("@role",a.Role,DbType.String,20);q.Param("@active",a.Active,DbType.Boolean);q.Param("@hash",a.Hash,DbType.String,255);
            try{accountId=Convert.ToInt32(q.ExecuteScalar());}catch(DbException ex) when(Db.IsDuplicate(ex)){throw new BusinessException("Tên đăng nhập đã tồn tại.","Username");}
        }
        using(var q=Db.Command(c,"UPDATE dbo.NhanVien SET MaTaiKhoan=@account,ChucVu=@role,HoTen=@name WHERE MaNhanVien=@employee",tx))
        {q.Param("@account",accountId,DbType.Int32);q.Param("@employee",employeeId,DbType.Int32);q.Param("@role",a.Role,DbType.String,20);q.Param("@name",a.FullName,DbType.String,100);q.ExecuteNonQuery();}
        tx.Commit();return accountId;
    }
}
