using System.Text.RegularExpressions;
using MocBakery.BLL.Services.Accounts;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.BLL.Services.Catalog;
public sealed class CatalogService(ILoaiBanhRepository categories, ISanPhamRepository products, AuthService auth)
{
    public IReadOnlyList<LoaiBanh> Categories(UserSession session, string keyword = "")
    {
        auth.Require(session);
        return categories.List().Where(x => x.TenLoai.Contains(keyword.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();
    }
    public IReadOnlyList<SanPham> Products(UserSession session, string keyword = "", int? categoryId = null, bool? active = null)
    {
        auth.Require(session);
        return products.List().Where(x => x.TenSanPham.Contains(keyword.Trim(), StringComparison.CurrentCultureIgnoreCase)
            && (categoryId is null || x.MaLoai == categoryId) && (active is null || x.DangKinhDoanh == active)).ToList();
    }
    public int SaveCategory(UserSession session, LoaiBanh input)
    {
        auth.Require(session, true);
        if (input.MaLoai < 0) throw new BusinessException("Mã loại không hợp lệ.");
        string name = Required(input.TenLoai, 100, "Tên loại", "Name");
        string? note = Optional(input.MoTa, 255, "Mô tả", "Note");
        if (categories.List().Any(x => x.MaLoai != input.MaLoai && string.Equals(x.TenLoai, name, StringComparison.CurrentCultureIgnoreCase)))
            throw new BusinessException("Tên loại bánh đã tồn tại.", "Name");
        return categories.Save(new(input.MaLoai, name, note));
    }
    public void DeleteCategory(UserSession session, int id)
    {
        auth.Require(session, true);
        if (id <= 0) throw new BusinessException("Chọn loại bánh trước.");
        categories.Delete(id);
    }
    public int SaveProduct(UserSession session, SanPham input)
    {
        auth.Require(session, true);
        if (input.MaSanPham < 0) throw new BusinessException("Mã sản phẩm không hợp lệ.");
        string name = Required(input.TenSanPham, 150, "Tên sản phẩm", "Name");
        string unit = Required(input.DonViTinh, 20, "Đơn vị tính", "Unit");
        string? note = Optional(input.MoTa, 500, "Mô tả", "Note");
        if (input.GiaBan < 0 || input.GiaBan > 9999999999999999.99m || decimal.Round(input.GiaBan, 2) != input.GiaBan)
            throw new BusinessException("Giá phải không âm, tối đa 16 chữ số nguyên và 2 chữ số thập phân.", "Price");
        if (!categories.List().Any(x => x.MaLoai == input.MaLoai))
            throw new BusinessException("Chọn loại bánh còn tồn tại.", "Category");
        if (products.List().Any(x => x.MaSanPham != input.MaSanPham && string.Equals(x.TenSanPham, name, StringComparison.CurrentCultureIgnoreCase)))
            throw new BusinessException("Tên sản phẩm đã tồn tại.", "Name");
        // Never delete products: keeping their identity preserves lot, invoice and order history.
        return products.Save(input with { TenSanPham = name, DonViTinh = unit, MoTa = note });
    }
    private static string Required(string value, int max, string label, string field)
    {
        string clean = Regex.Replace(value.Trim(), @"\s+", " ");
        if (clean.Length < 1 || clean.Length > max) throw new BusinessException($"{label} phải có 1–{max} ký tự.", field);
        return clean;
    }
    private static string? Optional(string? value, int max, string label, string field)
    {
        string? clean = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (clean?.Length > max) throw new BusinessException($"{label} tối đa {max} ký tự.", field);
        return clean;
    }
}
