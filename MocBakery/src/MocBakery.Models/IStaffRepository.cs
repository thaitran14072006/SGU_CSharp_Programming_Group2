using MocBakery.Models.Entities;
namespace MocBakery.Models;

public interface IStaffRepository
{
    IReadOnlyList<NhanVien> List();
    int Save(int actorId, NhanVien input);
    int SaveAccount(int actorId, int employeeId, int accountId, AccountData input);
}
