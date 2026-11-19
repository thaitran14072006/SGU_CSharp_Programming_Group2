using MocBakery.Models;
using MocBakery.Models.WebCart;
using MocBakery.Models.WebOrders;
namespace MocBakery.BLL.Services.Orders;

public sealed class WebOrderService(IWebOrderRepository repository) : IWebOrderSubmission
{
    public int Submit(WebPurchaseRequest request)
    {
        if(request.CustomerId<=0||request.RequestKey==Guid.Empty)
            throw new BusinessException("Phiên đặt hàng không hợp lệ. Vui lòng đăng nhập lại.");
        if(request.Items.Count is <1 or >50||request.Items.Any(x=>x.ProductId<=0||x.Quantity is <1 or >1000)
            ||request.Items.Select(x=>x.ProductId).Distinct().Count()!=request.Items.Count)
            throw new BusinessException("Giỏ hàng không hợp lệ. Vui lòng chọn lại bánh.");
        if(request.ConfirmedTotal<=0||request.ConfirmedTotal>9999999999999999.99m)
            throw new BusinessException("Tổng tiền không hợp lệ.");
        if(request.ConfirmedLines is null||request.ConfirmedLines.Count!=request.Items.Count
            ||request.ConfirmedLines.Select(x=>x.ProductId).Distinct().Count()!=request.Items.Count
            ||request.Items.Any(item=>!request.ConfirmedLines.Any(line=>line.ProductId==item.ProductId&&line.Quantity==item.Quantity&&line.UnitPrice>0)))
            throw new BusinessException("Giá xác nhận không hợp lệ. Vui lòng xem lại giỏ hàng.");
        if(request.Pickup is null)throw new BusinessException("Vui lòng chọn ngày giờ nhận bánh.");
        string? note=string.IsNullOrWhiteSpace(request.Note)?null:request.Note.Trim();
        if(note?.Length>500)throw new BusinessException("Yêu cầu tối đa 500 ký tự.");
        return repository.Submit(request with{Note=note});
    }
    public IReadOnlyList<CustomerOrder> List(int customerId)=>customerId>0?repository.List(customerId):[];
    public CustomerOrderDetail? Find(int customerId,int orderId)=>customerId>0&&orderId>0?repository.Find(customerId,orderId):null;
}
