using System.Text.RegularExpressions;
using MocBakery.BLL.Services.Accounts;
using MocBakery.Models;
using MocBakery.Models.CustomerAccounts;

namespace MocBakery.BLL.Services.CustomerAccounts;

public sealed class CustomerAccountService(ICustomerAccountRepository repository)
{
    private static readonly string DummyHash = PasswordHasher.Hash("customer-login-timing-only");

    public int Register(CustomerRegistration input)
    {
        string username = input.Username.Trim().ToLowerInvariant();
        if (!Regex.IsMatch(username, @"\A[a-z0-9][a-z0-9._-]{2,49}\z"))
            throw new BusinessException("Tên đăng nhập gồm 3–50 chữ cái không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang; bắt đầu bằng chữ hoặc số.", "Username");
        AuthService.ValidatePassword(input.Password);
        if (input.Password != input.Confirmation)
            throw new BusinessException("Xác nhận mật khẩu không khớp.", "Confirmation");
        var profile = Normalize(new(input.FullName, input.Phone, input.Address));
        return repository.Register(username, PasswordHasher.Hash(input.Password), profile);
    }

    public CustomerIdentity Login(string username, string password)
    {
        username = username.Trim().ToLowerInvariant();
        if (username.Length is < 3 or > 50 || password.Length is < 8 or > 64)
            throw new BusinessException("Tên đăng nhập hoặc mật khẩu không đúng.");
        var account = repository.FindByUsername(username);
        bool verified = PasswordHasher.Verify(password, account?.PasswordHash ?? DummyHash);
        if (account is null || !verified || !account.Active)
            throw new BusinessException("Tên đăng nhập hoặc mật khẩu không đúng.");
        return new(account.Id, account.CustomerId, account.SecurityStamp);
    }

    // The Web host must keep this identity in its authenticated, protected cookie.
    public CustomerAccount Require(CustomerIdentity identity)
    {
        var account = repository.FindById(identity.AccountId);
        if (identity.SecurityStamp == Guid.Empty || account is null || !account.Active ||
            account.CustomerId != identity.CustomerId || account.SecurityStamp != identity.SecurityStamp)
            throw new BusinessException("Phiên đã hết hiệu lực. Vui lòng đăng nhập lại.");
        return account;
    }

    public CustomerProfile Profile(CustomerIdentity identity)
    {
        Require(identity);
        return repository.ReadProfile(identity) ?? throw new BusinessException("Hồ sơ không còn khả dụng.");
    }

    public void UpdateProfile(CustomerIdentity identity, CustomerProfile input)
    {
        Require(identity);
        if (!repository.UpdateProfile(identity, Normalize(input)))
            throw new BusinessException("Phiên đã thay đổi. Vui lòng đăng nhập lại.");
    }

    public void ChangePassword(CustomerIdentity identity, string oldPassword, string password, string confirmation)
    {
        var account = Require(identity);
        AuthService.ValidatePassword(password);
        if (password != confirmation)
            throw new BusinessException("Xác nhận mật khẩu không khớp.", "Confirmation");
        if (oldPassword.Length > 64 || !PasswordHasher.Verify(oldPassword, account.PasswordHash))
            throw new BusinessException("Mật khẩu hiện tại không đúng.", "OldPassword");
        if (oldPassword == password)
            throw new BusinessException("Mật khẩu mới phải khác mật khẩu hiện tại.", "Password");
        if (!repository.ChangePassword(identity, account.PasswordHash, PasswordHasher.Hash(password)))
            throw new BusinessException("Tài khoản đã thay đổi. Vui lòng đăng nhập lại.");
    }

    private static CustomerProfile Normalize(CustomerProfile profile)
    {
        string name = Regex.Replace(profile.FullName.Trim(), @"\s+", " ");
        string phone = profile.Phone.Trim();
        string? address = string.IsNullOrWhiteSpace(profile.Address) ? null : profile.Address.Trim();
        if (name.Length is < 1 or > 100)
            throw new BusinessException("Họ tên phải có 1–100 ký tự.", "FullName");
        if (!Regex.IsMatch(phone, @"\A[0-9]{10}\z"))
            throw new BusinessException("Số điện thoại phải có đúng 10 chữ số.", "Phone");
        if (address?.Length > 255)
            throw new BusinessException("Địa chỉ tối đa 255 ký tự.", "Address");
        return new(name, phone, address);
    }
}
