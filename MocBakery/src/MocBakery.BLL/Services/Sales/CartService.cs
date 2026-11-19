using MocBakery.BLL.Services.Catalog;
using MocBakery.Models;
using MocBakery.Models.WebCart;

namespace MocBakery.BLL.Services.Sales;

public sealed class CartService(PublicCatalogService catalog)
{
    public IReadOnlyList<CartItem> Change(IReadOnlyList<CartItem> items, int productId, int quantity, bool add)
    {
        if (productId <= 0 || quantity is < 0 or > 1000 || (add && quantity == 0))
            throw new BusinessException("Số lượng phải từ 1 đến 1000; dùng nút Xóa để bỏ bánh.");
        var result = items.ToList();
        int index = result.FindIndex(x => x.ProductId == productId);
        int next = add ? quantity + (index < 0 ? 0 : result[index].Quantity) : quantity;
        if (next > 1000) throw new BusinessException("Mỗi bánh tối đa 1000 sản phẩm.");
        if (next == 0) { if (index >= 0) result.RemoveAt(index); return result; }
        var selection = catalog.CheckSelection(productId, next);
        if (selection is null || !selection.CanFulfill) throw new BusinessException("Bánh ngừng bán hoặc không đủ số lượng. Vui lòng chọn lại.");
        if (index < 0 && result.Count >= 50) throw new BusinessException("Giỏ hàng tối đa 50 loại bánh.");
        var item = new CartItem(productId, next);
        if (index < 0) result.Add(item); else result[index] = item;
        return result;
    }

    public CartQuote Quote(IReadOnlyList<CartItem> items)
    {
        if (items.Count > 50 || items.Select(x => x.ProductId).Distinct().Count() != items.Count)
            throw new BusinessException("Giỏ hàng không hợp lệ. Vui lòng chọn lại bánh.");
        return new(items.Select(item =>
        {
            var row = catalog.CheckSelection(item.ProductId, item.Quantity);
            return new CartLine(item.ProductId, row?.Name ?? "Bánh không còn mở bán", item.Quantity,
                row?.CurrentUnitPrice ?? 0, row?.CanFulfill == true);
        }).ToList());
    }
}
