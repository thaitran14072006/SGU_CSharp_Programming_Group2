using MocBakery.Models.Entities;
namespace MocBakery.Models.DTOs.Inventory;
public sealed record LotInput(int MaSanPham, string? NguonNhap, DateOnly NgaySanXuat,
    DateOnly HanSuDung, int SoLuongNhap);
public sealed record StockRow(int MaSanPham, string TenSanPham, string DonViTinh, bool DangKinhDoanh,
    long TongTon, long TonCoTheBan, long TonHetHan, long TonNgungBan);
public sealed record LotRow(int MaLo, int MaSanPham, string TenSanPham, DateTime NgayNhap,
    string? NguonNhap, DateOnly NgaySanXuat, DateOnly HanSuDung, int SoLuongNhap, int SoLuongCon,
    string TinhTrang, string TrangThaiHienThi, bool CoTheBan);
public static class InventoryRules
{
    // DATE is inclusive: a lot expires at the start of the following business day.
    public static bool CanSell(LoBanh lot, SanPham product, DateOnly businessDate) =>
        lot.MaSanPham == product.MaSanPham && product.DangKinhDoanh && lot.SoLuongCon > 0 &&
        lot.TinhTrang == LotStatus.Selling && lot.NgaySanXuat <= businessDate && lot.HanSuDung >= businessDate;
}
