using System.Text.RegularExpressions;
using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;
namespace MocBakery.BLL.Services.Accounts;

public sealed class StaffService(IStaffRepository repository, AuthService auth)
{
    public IReadOnlyList<NhanVien> Search(UserSession session, string keyword = "")
    {
        auth.Require(session, owner: true);
        keyword = keyword.Trim();
        return repository.List().Where(n => n.MaHienThi.Equals(keyword,StringComparison.OrdinalIgnoreCase) ||
            n.HoTen.Contains(keyword,StringComparison.OrdinalIgnoreCase) ||
            (n.SoDienThoai?.Contains(keyword) ?? false)).ToList();
    }

    public int Save(UserSession session, NhanVien input)
    {
        int actor = auth.Require(session, owner: true).MaTaiKhoan;
        string name = Regex.Replace(input.HoTen.Trim(), @"\s+", " ");
        string phone = input.SoDienThoai?.Trim() ?? "";
        if (name.Length is < 1 or > 100) throw new BusinessException("Họ tên phải có 1–100 ký tự.","FullName");
        if (!Regex.IsMatch(phone,@"^[0-9]{10}$")) throw new BusinessException("Điện thoại phải có đúng 10 chữ số.","Phone");
        if (!Roles.IsValid(input.ChucVu)) throw new BusinessException("Chức vụ không hợp lệ.","Role");
        if (input.NgayVaoLam > DateOnly.FromDateTime(DateTime.Today) || input.NgayVaoLam.Year < 1900)
            throw new BusinessException("Ngày vào làm phải từ năm 1900 đến hôm nay.","StartDate");
        return repository.Save(actor,input with {HoTen=name,SoDienThoai=phone});
    }
}
