using MocBakery.BLL.Services.Accounts;
using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Customers;
using MocBakery.Models.DTOs.Sales;
using MocBakery.Models.Entities;

namespace MocBakery.BLL.Services.Sales;

public sealed class SalesService(ISalesRepository repository, AuthService auth)
{
    public IReadOnlyList<SalesProduct> Products(UserSession session)
    {
        auth.RequireSales(session);
        return repository.ListProducts();
    }

    public IReadOnlyList<InvoiceSummary> Invoices(UserSession session)
    {
        auth.RequireSales(session);
        return repository.ListInvoices();
    }

    public IReadOnlyList<InvoiceLine> Invoice(UserSession session, int id)
    {
        auth.RequireSales(session);
        return repository.ReadInvoice(id);
    }

    public int Sell(UserSession session, int? customerId, IReadOnlyList<CartItem> cart)
    {
        int actor = auth.RequireSales(session).MaTaiKhoan;
        if (cart.Count == 0 || cart.Any(item => item.ProductId <= 0 || item.Quantity <= 0 || item.UnitPrice < 0))
            throw new BusinessException("Giỏ hàng trống hoặc có sản phẩm không hợp lệ.");
        if (cart.Select(item => item.ProductId).Distinct().Count() != cart.Count)
            throw new BusinessException("Mỗi sản phẩm chỉ được xuất hiện một lần trong giỏ.");
        return repository.Sell(actor, customerId, cart, Allocate);
    }

    public static IReadOnlyList<LotAllocation> Allocate(IEnumerable<LoBanh> lots, int quantity)
    {
        if (quantity <= 0) throw new BusinessException("Số lượng bán phải lớn hơn 0.");
        var allocations = new List<LotAllocation>();
        foreach (var lot in lots.Where(lot => lot.TinhTrang == LotStatus.Selling && lot.SoLuongCon > 0)
                     .OrderBy(lot => lot.HanSuDung).ThenBy(lot => lot.MaLo))
        {
            int taken = Math.Min(quantity, lot.SoLuongCon);
            allocations.Add(new(lot.MaLo, taken));
            quantity -= taken;
            if (quantity == 0) return allocations;
        }
        throw new BusinessException("Không đủ tồn hợp lệ. Hãy tải lại danh sách bánh.");
    }
}
