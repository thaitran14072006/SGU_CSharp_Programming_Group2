namespace MocBakery.Models.Entities;
public sealed record TaiKhoan(int MaTaiKhoan, string TenDangNhap, string MatKhauHash,
    string HoTen, string VaiTro, bool TrangThai, DateTime NgayTao);
public static class Roles
{
    public const string ChuTiem = "ChuTiem";
    public const string NhanVien = "NhanVien";
}
