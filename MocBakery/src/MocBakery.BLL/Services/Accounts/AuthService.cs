using System.Collections.Concurrent;
using System.Security.Cryptography;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.BLL.Services.Accounts;
public sealed class AuthService(IAccountRepository accounts)
{
    private readonly ConcurrentDictionary<string, (int Id, string Hash)> sessions = new();
    private readonly string dummyHash = PasswordHasher.Hash("dummy-password");
    public UserSession Login(string username, string password)
    {
        username = username.Trim();
        if (username.Length == 0) throw new BusinessException("Nhập tên đăng nhập.", "Username");
        if (password.Length == 0) throw new BusinessException("Nhập mật khẩu.", "Password");
        var account = accounts.FindByUsername(username);
        bool verified = PasswordHasher.Verify(password, account?.MatKhauHash ?? dummyHash);
        if (account is null || !verified)
            throw new BusinessException("Tên đăng nhập hoặc mật khẩu không đúng");
        if (!account.TrangThai) throw new BusinessException("Tài khoản không còn hoạt động.");
        if (!Roles.IsValid(account.VaiTro))
            throw new BusinessException("Vai trò tài khoản không hợp lệ.");
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        sessions[token] = (account.MaTaiKhoan, account.MatKhauHash);
        return new(token, account.MaTaiKhoan, account.TenDangNhap, account.HoTen, account.VaiTro);
    }
    // Public session data is display-only. Authority is reloaded from the database.
    public TaiKhoan Require(UserSession session, bool owner = false)
    {
        if (!sessions.TryGetValue(session.Token, out var issued))
            throw new BusinessException("Phiên đã kết thúc. Vui lòng đăng nhập lại.");
        var account = accounts.FindById(issued.Id);
        if (account is null || !account.TrangThai || account.MatKhauHash != issued.Hash)
        {
            Logout(session);
            throw new BusinessException("Tài khoản hoặc mật khẩu đã thay đổi. Vui lòng đăng nhập lại.");
        }
        if (!Roles.IsValid(account.VaiTro) || (owner && account.VaiTro != Roles.ChuTiem))
            throw new BusinessException("Chỉ chủ tiệm được thực hiện chức năng này.");
        return account;
    }
    public void Logout(UserSession session) => sessions.TryRemove(session.Token, out _);
    public void ChangePassword(UserSession session, string oldPassword, string newPassword, string confirmation)
    {
        var current = Require(session);
        ValidatePassword(newPassword);
        if (newPassword != confirmation) throw new BusinessException("Xác nhận mật khẩu không khớp.", "Confirmation");
        if (!PasswordHasher.Verify(oldPassword, current.MatKhauHash))
            throw new BusinessException("Mật khẩu hiện tại không đúng.", "OldPassword");
        if (oldPassword == newPassword) throw new BusinessException("Mật khẩu mới phải khác mật khẩu hiện tại.", "Password");
        string hash = PasswordHasher.Hash(newPassword);
        if (!accounts.ChangePassword(current.MaTaiKhoan, current.MatKhauHash, hash))
            throw new BusinessException("Tài khoản đã thay đổi. Hãy đăng nhập lại.");
        sessions[session.Token] = (current.MaTaiKhoan, hash);
        // Keep this session per UC02; other sessions are invalidated by the changed hash.
    }
    public static void ValidatePassword(string password)
    {
        if (password.Length is < 8 or > 64)
            throw new BusinessException("Mật khẩu phải có 8–64 ký tự.", "Password");
    }
}
