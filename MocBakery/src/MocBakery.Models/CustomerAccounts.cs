namespace MocBakery.Models.CustomerAccounts;

// Employee accounts and permissions remain in TaiKhoan. These records are customer-only.
public sealed record CustomerAccount(int Id, int CustomerId, string Username, string PasswordHash,
    bool Active, Guid SecurityStamp);
public sealed record CustomerIdentity(int AccountId, int CustomerId, Guid SecurityStamp);
public sealed record CustomerProfile(string FullName, string Phone, string? Address);
public sealed record CustomerRegistration(string Username, string Password, string Confirmation,
    string FullName, string Phone, string? Address);

public interface ICustomerAccountRepository
{
    CustomerAccount? FindByUsername(string username);
    CustomerAccount? FindById(int id);
    int Register(string username, string passwordHash, CustomerProfile profile);
    CustomerProfile? ReadProfile(CustomerIdentity identity);
    bool UpdateProfile(CustomerIdentity identity, CustomerProfile profile);
    bool ChangePassword(CustomerIdentity identity, string expectedHash, string newHash);
}
