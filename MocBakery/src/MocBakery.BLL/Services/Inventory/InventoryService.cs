using MocBakery.BLL.Services.Accounts;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Inventory;
namespace MocBakery.BLL.Services.Inventory;
public sealed class InventoryService(ILoBanhRepository lots, ISanPhamRepository products, AuthService auth,
    Func<DateOnly>? businessDate = null)
{
    private DateOnly Today => businessDate?.Invoke() ?? DateOnly.FromDateTime(DateTime.Now);
    public int Receive(UserSession session, LotInput input)
    {
        auth.Require(session, true);
        if (!products.List().Any(x => x.MaSanPham == input.MaSanPham && x.DangKinhDoanh))
            throw new BusinessException("Chọn sản phẩm đang kinh doanh.", "Product");
        if (input.SoLuongNhap <= 0) throw new BusinessException("Số lượng nhập phải lớn hơn 0.", "Quantity");
        if (input.NgaySanXuat > Today) throw new BusinessException("Ngày sản xuất không được ở tương lai.", "Produced");
        if (input.HanSuDung < input.NgaySanXuat) throw new BusinessException("Hạn dùng không được trước ngày sản xuất.", "Expiry");
        string? source = string.IsNullOrWhiteSpace(input.NguonNhap) ? null : input.NguonNhap.Trim();
        if (source?.Length > 100) throw new BusinessException("Nguồn nhập tối đa 100 ký tự.", "Source");
        return lots.Receive(input with { NguonNhap = source });
    }
    public void SetStatus(UserSession session, int lotId, string status)
    {
        auth.Require(session, true);
        if (lotId <= 0) throw new BusinessException("Chọn lô bánh trước.");
        if (status is not (LotStatus.Selling or LotStatus.Stopped)) throw new BusinessException("Trạng thái lô không hợp lệ.");
        lots.SetStatus(lotId, status);
    }
    public IReadOnlyList<LoBanh> SellableLots(UserSession session, int productId)
    {
        auth.Require(session);
        if (productId <= 0) throw new BusinessException("Chọn sản phẩm trước.");
        return lots.GetSellableLots(productId, Today);
    }
    public IReadOnlyList<StockRow> Stock(UserSession session, string keyword = "")
    {
        auth.Require(session);
        var date = Today;
        var grouped = lots.List().ToLookup(x => x.MaSanPham);
        return products.List().Where(p => p.TenSanPham.Contains(keyword.Trim(), StringComparison.CurrentCultureIgnoreCase))
            .Select(p => new StockRow(p.MaSanPham, p.TenSanPham, p.DonViTinh, p.DangKinhDoanh,
                grouped[p.MaSanPham].Sum(l => (long)l.SoLuongCon),
                grouped[p.MaSanPham].Where(l => InventoryRules.CanSell(l, p, date)).Sum(l => (long)l.SoLuongCon),
                grouped[p.MaSanPham].Where(l => l.HanSuDung < date).Sum(l => (long)l.SoLuongCon),
                grouped[p.MaSanPham].Where(l => l.TinhTrang == LotStatus.Stopped).Sum(l => (long)l.SoLuongCon))).ToList();
    }
    public IReadOnlyList<LotRow> Lots(UserSession session, int? productId = null, string filter = "All")
    {
        auth.Require(session);
        var date = Today;
        var allProducts = products.List().ToDictionary(p => p.MaSanPham);
        return lots.List().Where(l => productId is null || l.MaSanPham == productId)
            .Select(l => {
                var p = allProducts[l.MaSanPham];
                bool sellable = InventoryRules.CanSell(l, p, date);
                string label = !p.DangKinhDoanh ? "Sản phẩm ngừng kinh doanh" : l.TinhTrang == LotStatus.Stopped ? "Ngừng bán"
                    : l.SoLuongCon == 0 ? "Hết hàng" : l.HanSuDung < date ? "Hết hạn" : l.NgaySanXuat > date ? "Chưa đến ngày sản xuất" : "Có thể bán";
                return new LotRow(l.MaLo, l.MaSanPham, p.TenSanPham, l.NgayNhap, l.NguonNhap, l.NgaySanXuat,
                    l.HanSuDung, l.SoLuongNhap, l.SoLuongCon, l.TinhTrang, label, sellable);
            })
            .Where(l => filter switch { "Sellable" => l.CoTheBan, "Expired" => l.HanSuDung < date,
                "Stopped" => l.TinhTrang == LotStatus.Stopped, _ => true })
            .OrderBy(l => l.HanSuDung).ThenBy(l => l.MaLo).ToList();
    }
}
