namespace MocBakery.Models.Entities;
public sealed record SanPham(int MaSanPham, int MaLoai, string TenSanPham, string DonViTinh,
    decimal GiaBan, bool DangKinhDoanh, string? MoTa, int HanDungNgay = 1, byte[]? AnhBanh = null, decimal NguongCanhBao = 5);
