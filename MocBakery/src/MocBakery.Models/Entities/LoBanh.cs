namespace MocBakery.Models.Entities;
public static class LotStatus
{
    public const string Selling = "DangBan", Stopped = "NgungBan";
}
public sealed record LoBanh(int MaLo, int MaSanPham, DateTime NgayNhap, string? NguonNhap,
    DateOnly NgaySanXuat, DateOnly HanSuDung, int SoLuongNhap, int SoLuongCon, string TinhTrang);
