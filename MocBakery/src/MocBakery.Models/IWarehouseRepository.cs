using MocBakery.Models.DTOs.Inventory;
namespace MocBakery.Models;
public interface IWarehouseRepository
{
    IReadOnlyList<Supplier> Suppliers();
    int SaveSupplier(Supplier value);
    IReadOnlyList<Material> Materials();
    int SaveMaterial(Material value);
    IReadOnlyList<WarehouseItem> Items();
    IReadOnlyList<WarehouseLot> Lots();
    IReadOnlyList<ReceiptSummary> Receipts();
    Receipt GetReceipt(int id);
    int SaveDraft(int actor,Receipt receipt);
    void Post(int actor,int id);
    void ChangeStock(int actor,StockChange change);
    IReadOnlyList<StockHistory> History(string kind,int lotId);
}
