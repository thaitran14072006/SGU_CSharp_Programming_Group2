using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.BLL.Services.Accounts;
public sealed class AccountService(IAccountRepository accounts, AuthService auth)
{
    public IReadOnlyList<AccountSummary> List(UserSession session)
    {
        auth.Require(session, owner: true);
        return accounts.List().Select(a => new AccountSummary(a.MaTaiKhoan, a.TenDangNhap, a.HoTen, a.VaiTro, a.TrangThai)).ToList();
    }
    public int Save(UserSession session, int id, AccountInput input)
    {
        var actor = auth.Require(session, owner: true);
        string username = input.TenDangNhap.Trim(), name = input.HoTen.Trim();
        if (username.Length is < 3 or > 50) throw new BusinessException("Tên đăng nhập phải có 3–50 ký tự.", "Username");
        if (name.Length is < 1 or > 100) throw new BusinessException("Họ tên phải có 1–100 ký tự.", "FullName");
        if (!Roles.IsValid(input.VaiTro))
            throw new BusinessException("Vai trò không hợp lệ.", "Role");
        var duplicate = accounts.FindByUsername(username);
        if (duplicate is not null && duplicate.MaTaiKhoan != id)
            throw new BusinessException("Tên đăng nhập đã tồn tại.", "Username");
        string hash;
        if (id == 0)
        {
            AuthService.ValidatePassword(input.Password);
            hash = PasswordHasher.Hash(input.Password);
        }
        else
        {
            var original = accounts.FindById(id) ?? throw new BusinessException("Tài khoản không còn tồn tại.");
            if (username != original.TenDangNhap)
                throw new BusinessException("Không đổi tên đăng nhập của tài khoản đã tạo.", "Username");
            hash = original.MatKhauHash;
            if (actor.MaTaiKhoan == id && (!input.TrangThai || input.VaiTro != Roles.ChuTiem))
                throw new BusinessException("Không tự khóa hoặc hạ quyền tài khoản đang sử dụng.", "Active");
            if (original.TrangThai && original.VaiTro == Roles.ChuTiem &&
                (!input.TrangThai || input.VaiTro != Roles.ChuTiem) &&
                accounts.List().Count(a => a.TrangThai && a.VaiTro == Roles.ChuTiem) <= 1)
                throw new BusinessException("Phải giữ ít nhất một chủ tiệm hoạt động.", "Active");
        }
        var data = new AccountData(username, name, input.VaiTro, id == 0 || input.TrangThai, hash);
        if (id == 0) return accounts.Create(data);
        accounts.Update(actor.MaTaiKhoan, id, data);
        return id;
    }
    public void ResetPassword(UserSession session, int id, string password)
    {
        var actor = auth.Require(session, owner: true);
        if (id == actor.MaTaiKhoan)
            throw new BusinessException("Dùng chức năng Đổi mật khẩu cho tài khoản của bạn.");
        AuthService.ValidatePassword(password);
        var account = accounts.FindById(id) ?? throw new BusinessException("Chọn tài khoản trước.");
        if (!accounts.ChangePassword(id, account.MatKhauHash, PasswordHasher.Hash(password), requireActive: false))
            throw new BusinessException("Tài khoản đã thay đổi. Hãy tải lại danh sách.");
    }
}
