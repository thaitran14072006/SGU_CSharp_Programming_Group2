using MocBakery.Models.WebCart;
using System.ComponentModel.DataAnnotations;
namespace MocBakery.Web.Models;

public sealed record CartViewModel(CartQuote Quote, bool OrderServiceReady = false, Guid RequestKey = default);
public sealed record CartReview(int CustomerId, Guid RequestKey, CartQuote Quote);
public sealed record CheckoutDraft(DateTime? Pickup, string? Note, string? FullName=null, string? Phone=null, string? Email=null);

public class CheckoutForm
{
    public Guid RequestKey { get; set; }
    [Required(ErrorMessage="Vui lòng nhập họ tên người nhận."),StringLength(100,ErrorMessage="Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; }="";
    [Required(ErrorMessage="Vui lòng nhập số điện thoại."),RegularExpression(@"[0-9]{10}",ErrorMessage="Số điện thoại phải gồm đúng 10 chữ số.")]
    public string Phone { get; set; }="";
    [Required(ErrorMessage="Vui lòng nhập email."),EmailAddress(ErrorMessage="Email chưa đúng định dạng."),StringLength(254,ErrorMessage="Email tối đa 254 ký tự.")]
    public string Email { get; set; }="";
    [Required(ErrorMessage="Vui lòng chọn ngày giờ nhận bánh.")]
    public DateTime? Pickup { get; set; }
    [StringLength(500,ErrorMessage="Yêu cầu thêm tối đa 500 ký tự.")]
    public string? Note { get; set; }
}

public sealed class CheckoutViewModel : CheckoutForm
{
    public required CartQuote Quote { get; init; }
    public static CheckoutViewModel Create(CartQuote quote,CheckoutForm form)=>new()
    {
        Quote=quote,RequestKey=form.RequestKey,FullName=form.FullName,Phone=form.Phone,Email=form.Email,Pickup=form.Pickup,Note=form.Note
    };
}
