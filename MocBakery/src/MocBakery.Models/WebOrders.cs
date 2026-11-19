using MocBakery.Models.DTOs.Orders;
using MocBakery.Models.WebCart;
namespace MocBakery.Models.WebOrders;

public sealed record CustomerOrder(int Id, DateTime Placed, DateTime Pickup, decimal Total,
    string Status, string? Note, string? CancelReason)
{
    public string StatusText => Status switch
    {
        "MoiTiepNhan" => "Mới tiếp nhận", "DangChuanBi" => "Đang chuẩn bị",
        "DaGiao" => "Đã giao", "DaHuy" => "Đã hủy", _ => "Đang cập nhật"
    };
}
public sealed record CustomerOrderDetail(CustomerOrder Order, IReadOnlyList<OrderLine> Lines);
public interface IWebOrderRepository
{
    int Submit(WebPurchaseRequest request);
    IReadOnlyList<CustomerOrder> List(int customerId);
    CustomerOrderDetail? Find(int customerId, int orderId);
}
