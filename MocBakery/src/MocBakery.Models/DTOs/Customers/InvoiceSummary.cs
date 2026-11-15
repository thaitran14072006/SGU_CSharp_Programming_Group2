namespace MocBakery.Models.DTOs.Customers;
public sealed record InvoiceSummary(int MaHoaDon, DateTime NgayLap, decimal TongTien, decimal TienCocTru, decimal SoTienThu)
{
    public string CustomerName { get; init; } = "Khách lẻ";
    public string Phone { get; init; } = "";
}
