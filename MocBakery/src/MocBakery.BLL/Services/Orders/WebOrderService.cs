using MocBakery.Models;
using MocBakery.Models.WebCart;
using MocBakery.Models.WebOrders;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
namespace MocBakery.BLL.Services.Orders;

public sealed class WebOrderService(IWebOrderRepository repository) : IWebOrderSubmission
{
    public void Cancel(int customerId,int orderId,string? reason)
    {
        if(customerId<=0||orderId<=0)throw new BusinessException("Đơn hàng không hợp lệ.");
        reason=string.IsNullOrWhiteSpace(reason)?"Khách hủy đơn trên Web.":reason.Trim();
        if(reason.Length>255)throw new BusinessException("Lý do hủy tối đa 255 ký tự.");
        if(!repository.Cancel(customerId,orderId,reason))
            throw new BusinessException("Không thể hủy đơn. Chỉ được hủy đơn Web mới tiếp nhận, chưa có cọc hoặc hóa đơn. Vui lòng tải lại trạng thái.");
    }
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
        var contact=request.Contact??throw new BusinessException("Vui lòng nhập đủ họ tên, số điện thoại và email.");
        string name=Regex.Replace(contact.FullName?.Trim()??"",@"\s+"," ");
        string phone=contact.Phone?.Trim()??"",email=contact.Email?.Trim()??"";
        if(name.Length is <1 or >100)throw new BusinessException("Họ tên phải có từ 1 đến 100 ký tự.","FullName");
        if(!Regex.IsMatch(phone,@"\A[0-9]{10}\z"))throw new BusinessException("Số điện thoại phải gồm đúng 10 chữ số.","Phone");
        if(email.Length is <3 or >254||!new EmailAddressAttribute().IsValid(email))throw new BusinessException("Email chưa đúng định dạng.","Email");
        return repository.Submit(request with{Note=note,Contact=new(name,phone,email)});
    }
    public IReadOnlyList<CustomerOrder> List(int customerId)=>customerId>0?repository.List(customerId):[];
    public CustomerOrderDetail? Find(int customerId,int orderId)=>customerId>0&&orderId>0?repository.Find(customerId,orderId):null;
}
