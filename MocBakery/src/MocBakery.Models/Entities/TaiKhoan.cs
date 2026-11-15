namespace MocBakery.Models.Entities;
public sealed record TaiKhoan(int MaTaiKhoan, string TenDangNhap, string MatKhauHash,
    string HoTen, string VaiTro, bool TrangThai, DateTime NgayTao);
public static class Roles
{
    public const string ChuTiem = "ChuTiem";
    public const string NhanVien = "NhanVien";
    public const string ThuNgan = "ThuNgan", Kho = "Kho";
    public static bool IsValid(string role) => role is ChuTiem or NhanVien or ThuNgan or Kho;
    public static bool CanManageInventory(string role) => role is ChuTiem or Kho;
    public static string Display(string role) => role switch { ChuTiem => "Quản lý", ThuNgan => "Thu ngân", Kho => "Nhân viên kho", _ => "Nhân viên" };
}
