namespace MocBakery.Models.Entities;

public sealed record NhanVien(int MaNhanVien, string HoTen, string? SoDienThoai,
    string ChucVu, DateOnly NgayVaoLam, bool DangLam, int? MaTaiKhoan = null)
{
    public string MaHienThi => $"NV{MaNhanVien:000}";
}
