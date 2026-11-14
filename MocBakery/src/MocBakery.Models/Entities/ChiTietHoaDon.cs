namespace MocBakery.Models.Entities;

public class ChiTietHoaDon
{
    public int MaCTHD { get; set; }
    public int MaHD { get; set; }
    public int MaSP { get; set; }
    public int MaLo { get; set; }          // bán từ lô nào (bắt buộc cho FEFO)
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }    // đơn giá tại thời điểm bán, không lấy lại từ SanPham
    public decimal ThanhTien => SoLuong * DonGia;
}