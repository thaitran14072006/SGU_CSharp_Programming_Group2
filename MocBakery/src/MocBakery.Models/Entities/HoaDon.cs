namespace MocBakery.Models.Entities;

public class HoaDon
{
    public int MaHD { get; set; }
    public DateTime NgayLap { get; set; }
    public int MaTK { get; set; }          // người lập hóa đơn (tài khoản)
    public int? MaKH { get; set; }         // khách hàng, có thể không có (bán lẻ)
    public int? MaDon { get; set; }        // gắn với đơn đặt nếu hóa đơn sinh từ giao đơn
    public decimal TongTien { get; set; }

    public List<ChiTietHoaDon> ChiTiet { get; set; } = new();
}