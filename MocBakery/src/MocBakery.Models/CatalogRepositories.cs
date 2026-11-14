using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Inventory;
namespace MocBakery.Models;
public interface ILoaiBanhRepository
{
    IReadOnlyList<LoaiBanh> List();
    int Save(LoaiBanh input);
    void Delete(int id);
}
public interface ISanPhamRepository
{
    IReadOnlyList<SanPham> List();
    int Save(SanPham input);
}
public interface ILoBanhRepository
{
    IReadOnlyList<LoBanh> List();
    int Receive(LotInput input);
    void SetStatus(int id, string status);
    IReadOnlyList<LoBanh> GetSellableLots(int productId, DateOnly businessDate);
}
