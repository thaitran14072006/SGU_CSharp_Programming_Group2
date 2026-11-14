namespace MocBakery.Models.Entities;
public sealed record SanPham(int MaSanPham, int MaLoai, string TenSanPham, string DonViTinh,
    decimal GiaBan, bool DangKinhDoanh, string? MoTa);
