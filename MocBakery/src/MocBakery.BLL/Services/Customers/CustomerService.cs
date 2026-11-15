using System.Text.RegularExpressions;
using MocBakery.BLL.Services.Accounts;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Customers;
namespace MocBakery.BLL.Services.Customers;
public sealed class CustomerService(ICustomerRepository customers, AuthService auth)
{
    public IReadOnlyList<KhachHang> Search(UserSession session, string keyword)
    {
        auth.RequireSales(session);
        return customers.Search(keyword.Trim());
    }
    public int Save(UserSession session, KhachHang input)
    {
        auth.RequireSales(session);
        string name = Regex.Replace(input.HoTen.Trim(), @"\s+", " ");
        string phone = input.SoDienThoai.Trim();
        if (name.Length is < 1 or > 100) throw new BusinessException("Họ tên phải có 1–100 ký tự.", "FullName");
        if (!Regex.IsMatch(phone, @"^[0-9]{10}$")) throw new BusinessException("Số điện thoại phải có đúng 10 chữ số.", "Phone");
        string? address = Clean(input.DiaChi), note = Clean(input.GhiChu);
        if (address?.Length > 255) throw new BusinessException("Địa chỉ tối đa 255 ký tự.", "Address");
        if (note?.Length > 255) throw new BusinessException("Ghi chú tối đa 255 ký tự.", "Note");
        if (customers.Search(phone).Any(c => c.SoDienThoai == phone && c.MaKhachHang != input.MaKhachHang))
            throw new BusinessException("Số điện thoại đã tồn tại. Sử dụng khách hàng hiện có.", "Phone");
        return customers.Save(new(input.MaKhachHang, name, phone, address, note));
    }
    public IReadOnlyList<InvoiceSummary> History(UserSession session, int id)
    {
        auth.RequireSales(session);
        if (id <= 0) throw new BusinessException("Chọn khách hàng trước.");
        return customers.History(id);
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
