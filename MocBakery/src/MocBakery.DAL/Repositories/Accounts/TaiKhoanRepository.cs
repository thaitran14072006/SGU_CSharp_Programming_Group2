using System.Data;
using System.Data.Common;
using MocBakery.Models;
using MocBakery.Models.Entities;
namespace MocBakery.DAL.Repositories.Accounts;
public sealed class TaiKhoanRepository(DbConnectionFactory db) : IAccountRepository
{
    private const string Columns = "MaTaiKhoan,TenDangNhap,MatKhauHash,HoTen,VaiTro,TrangThai,NgayTao";
    private static TaiKhoan Read(DbDataReader r) => new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetBoolean(5),r.GetDateTime(6));
    public TaiKhoan? FindByUsername(string username)
    {
        using var c = db.Open(); using var q = Db.Command(c, $"SELECT {Columns} FROM dbo.TaiKhoan WHERE TenDangNhap=@u");
        q.Param("@u", username, DbType.String, 50); using var r = q.ExecuteReader();
        return r.Read() ? Read(r) : null;
    }
    public TaiKhoan? FindById(int id)
    {
        using var c = db.Open(); using var q = Db.Command(c, $"SELECT {Columns} FROM dbo.TaiKhoan WHERE MaTaiKhoan=@id");
        q.Param("@id", id, DbType.Int32); using var r = q.ExecuteReader();
        return r.Read() ? Read(r) : null;
    }
    public IReadOnlyList<TaiKhoan> List()
    {
        using var c = db.Open(); using var q = Db.Command(c, $"SELECT {Columns} FROM dbo.TaiKhoan ORDER BY MaTaiKhoan");
        using var r = q.ExecuteReader(); var result = new List<TaiKhoan>();
        while (r.Read()) result.Add(Read(r)); return result;
    }
    private static void Params(DbCommand q, AccountData a)
    {
        q.Param("@u",a.Username,DbType.String,50); q.Param("@name",a.FullName,DbType.String,100);
        q.Param("@role",a.Role,DbType.String,20); q.Param("@active",a.Active,DbType.Boolean);
    }
    public int Create(AccountData input)
    {
        using var c = db.Open();
        return Insert(c, null, input);
    }
    private static int Insert(DbConnection c, DbTransaction? tx, AccountData input)
    {
        using var q = Db.Command(c, "INSERT dbo.TaiKhoan(TenDangNhap,MatKhauHash,HoTen,VaiTro,TrangThai) OUTPUT INSERTED.MaTaiKhoan VALUES(@u,@hash,@name,@role,@active)", tx);
        Params(q,input); q.Param("@hash",input.Hash,DbType.String,255);
        try { return Convert.ToInt32(q.ExecuteScalar()); }
        catch (DbException ex) when (Db.IsDuplicate(ex)) { throw new BusinessException("Tên đăng nhập đã tồn tại.", "Username"); }
    }
    public int Bootstrap(AccountData input)
    {
        using var c = db.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        using (var count = Db.Command(c,"SELECT COUNT(*) FROM dbo.TaiKhoan WITH (UPDLOCK,HOLDLOCK)",tx))
            if (Convert.ToInt32(count.ExecuteScalar()) != 0) throw new BusinessException("Đã có tài khoản. Chỉ khởi tạo khi bảng TaiKhoan rỗng.");
        int id = Insert(c,tx,input); tx.Commit(); return id;
    }
    public void Update(int actorId, int id, AccountData input)
    {
        using var c = db.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        List<TaiKhoan> rows = [];
        using (var read = Db.Command(c,$"SELECT {Columns} FROM dbo.TaiKhoan WITH (UPDLOCK,HOLDLOCK) ORDER BY MaTaiKhoan",tx))
        using (var r = read.ExecuteReader()) { while(r.Read()) rows.Add(Read(r)); }
        if (!rows.Any(a => a.MaTaiKhoan == actorId && a.TrangThai && a.VaiTro == Roles.ChuTiem))
            throw new BusinessException("Tài khoản hiện tại không còn quyền chủ tiệm.");
        var original = rows.Find(a => a.MaTaiKhoan == id) ?? throw new BusinessException("Tài khoản không còn tồn tại.");
        if ((actorId == id || rows.Count(a => a.TrangThai && a.VaiTro == Roles.ChuTiem) == 1 && original.TrangThai && original.VaiTro == Roles.ChuTiem)
            && (!input.Active || input.Role != Roles.ChuTiem))
            throw new BusinessException("Không thể khóa hoặc hạ quyền chủ tiệm này.", "Active");
        using var q = Db.Command(c,"UPDATE dbo.TaiKhoan SET HoTen=@name,VaiTro=@role,TrangThai=@active WHERE MaTaiKhoan=@id",tx);
        q.Param("@name",input.FullName,DbType.String,100); q.Param("@role",input.Role,DbType.String,20);
        q.Param("@active",input.Active,DbType.Boolean); q.Param("@id",id,DbType.Int32);
        q.ExecuteNonQuery(); tx.Commit();
    }
    public bool ChangePassword(int id, string expectedHash, string newHash, bool requireActive = true)
    {
        using var c = db.Open();
        using var q = Db.Command(c,"UPDATE dbo.TaiKhoan SET MatKhauHash=@new WHERE MaTaiKhoan=@id AND MatKhauHash COLLATE Latin1_General_100_BIN2=@old AND (@requireActive=0 OR TrangThai=1)");
        q.Param("@requireActive",requireActive,DbType.Boolean);
        q.Param("@id",id,DbType.Int32); q.Param("@old",expectedHash,DbType.String,255); q.Param("@new",newHash,DbType.String,255);
        return q.ExecuteNonQuery() == 1;
    }
}
