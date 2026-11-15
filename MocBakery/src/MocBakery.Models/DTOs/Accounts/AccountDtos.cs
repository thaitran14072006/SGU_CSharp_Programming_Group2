namespace MocBakery.Models.DTOs.Accounts;
// Hashes remain inside repositories/services; never bind TaiKhoan directly to a grid.
public sealed record AccountSummary(int MaTaiKhoan, string TenDangNhap, string HoTen, string VaiTro, bool TrangThai);
public sealed record AccountInput(string TenDangNhap, string HoTen, string VaiTro, bool TrangThai, string Password);
public sealed record UserSession(string Token, int MaTaiKhoan, string TenDangNhap, string HoTen, string VaiTro);
