using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Customers;
namespace MocBakery.Models;
public sealed class BusinessException(string message, string? field = null) : Exception(message)
{
    public string? Field { get; } = field;
}
public interface IAccountRepository
{
    TaiKhoan? FindByUsername(string username);
    TaiKhoan? FindById(int id);
    IReadOnlyList<TaiKhoan> List();
    int Create(AccountData input);
    void Update(int actorId, int id, AccountData input);
    bool ChangePassword(int id, string expectedHash, string newHash, bool requireActive = true);
}
public sealed record AccountData(string Username, string FullName, string Role, bool Active, string Hash);
public interface ICustomerRepository
{
    IReadOnlyList<KhachHang> Search(string keyword);
    int Save(KhachHang customer);
    IReadOnlyList<InvoiceSummary> History(int customerId);
}
