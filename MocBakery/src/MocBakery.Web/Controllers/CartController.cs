using System.Text.Json;
using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MocBakery.BLL.Services.Sales;
using MocBakery.Models;
using MocBakery.Models.WebCart;
using MocBakery.Web.Models;
using MocBakery.Web.Security;

namespace MocBakery.Web.Controllers;

[Route("gio-hang"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(16384)]
public sealed class CartController(CartService cart, IWebOrderSubmission orders, ILogger<CartController> logger) : Controller
{
    private const string ItemsKey = "Mai.Cart.Items";
    private const string ReviewKey = "Mai.Cart.Review";
    private const string LastOrderKey = "Mai.Cart.LastOrder";
    private IReadOnlyList<CartItem> Items => Read<List<CartItem>>(ItemsKey) ?? [];
    private T? Read<T>(string key) => HttpContext.Session.GetString(key) is { } json ? JsonSerializer.Deserialize<T>(json) : default;
    private void Write<T>(string key, T value) => HttpContext.Session.SetString(key, JsonSerializer.Serialize(value));
    private void Change(int productId, int quantity, bool add)
    {
        try { Write(ItemsKey, cart.Change(Items, productId, quantity, add)); HttpContext.Session.Remove(ReviewKey); }
        catch (BusinessException ex) { TempData["CartError"] = ex.Message; }
    }

    [HttpGet("")]
    public IActionResult Index() => View(new CartViewModel(cart.Quote(Items)));

    [HttpPost("them")]
    public IActionResult Add(int productId, int quantity) { if (ModelState.IsValid) Change(productId, quantity, true); else TempData["CartError"] = "Số lượng không hợp lệ."; return RedirectToAction(nameof(Index)); }

    [HttpPost("cap-nhat")]
    public IActionResult Update(int productId, int quantity) { if (ModelState.IsValid && quantity > 0) Change(productId, quantity, false); else TempData["CartError"] = "Số lượng phải lớn hơn 0."; return RedirectToAction(nameof(Index)); }

    [HttpPost("xoa")]
    public IActionResult Remove(int productId) { Change(productId, 0, false); return RedirectToAction(nameof(Index)); }

    [Authorize, HttpGet("xac-nhan")]
    public IActionResult Review()
    {
        var identity = CustomerCookie.Identity(User)!;
        var quote = cart.Quote(Items);
        if (!quote.CanSubmit) { TempData["CartError"] = "Giỏ trống hoặc có bánh không còn đủ số lượng. Vui lòng kiểm tra lại."; return RedirectToAction(nameof(Index)); }
        // Preserve the key across refreshes so concurrent retries reach Nam with the same key.
        var old = Read<CartReview>(ReviewKey);
        var review = old is not null && old.CustomerId == identity.CustomerId && Same(old.Quote, quote)
            ? old : new CartReview(identity.CustomerId, Guid.NewGuid(), quote);
        Write(ReviewKey, review);
        ViewData["CheckoutDraft"]=Read<CheckoutDraft>("Mai.Cart.Draft");
        return View(new CartViewModel(quote, true, review.RequestKey));
    }

    [Authorize, HttpPost("xac-nhan")]
    public IActionResult Confirm(Guid requestKey, DateTime? pickup, string? note)
    {
        var identity = CustomerCookie.Identity(User)!;
        Write("Mai.Cart.Draft",new CheckoutDraft(pickup,note));
        var last=Read<CompletedOrder>(LastOrderKey);
        if(requestKey!=Guid.Empty&&last?.CustomerId==identity.CustomerId&&last.RequestKey==requestKey)
            return RedirectToAction("Detail","CustomerOrders",new{id=last.OrderId});
        var review = Read<CartReview>(ReviewKey);
        if (!ModelState.IsValid || requestKey == Guid.Empty || review is null || review.CustomerId != identity.CustomerId || review.RequestKey != requestKey)
        { TempData["CartError"] = "Phiên xác nhận đã thay đổi. Vui lòng kiểm tra lại giỏ."; return RedirectToAction(nameof(Index)); }
        var quote = cart.Quote(Items);
        if (!quote.CanSubmit || !Same(review.Quote, quote))
        { HttpContext.Session.Remove(ReviewKey); TempData["CartError"] = "Giá hoặc tình trạng bánh vừa thay đổi. Vui lòng xem lại và xác nhận mức giá mới."; return RedirectToAction(nameof(Index)); }
        if(pickup is null||pickup<=DateTime.Now||note?.Length>500)
        {TempData["CartError"]="Chọn ngày giờ nhận trong tương lai và yêu cầu tối đa 500 ký tự.";return RedirectToAction(nameof(Review));}
        try
        {
            // Only server-owned customer identity, quantities and rechecked total cross this boundary.
            int id = orders.Submit(new(identity.CustomerId, requestKey, Items, quote.Total, pickup, note, quote.Lines));
            if (id <= 0) throw new BusinessException("Chưa nhận được mã đơn. Vui lòng thử lại.");
            HttpContext.Session.Remove(ItemsKey); HttpContext.Session.Remove(ReviewKey);
            HttpContext.Session.Remove("Mai.Cart.Draft");
            Write(LastOrderKey,new CompletedOrder(identity.CustomerId,requestKey,id));
            TempData["CartSuccess"] = $"Đã gửi yêu cầu mua thành công. Mã đơn: {id}. Tiệm sẽ tiếp nhận và xử lý đơn của bạn.";
            return RedirectToAction("Detail","CustomerOrders",new{id});
        }
        catch (BusinessException ex) { TempData["CartError"] = ex.Message; return RedirectToAction(nameof(Review)); }
        catch(Exception ex) when(ex is DbException or InvalidOperationException)
        {logger.LogError(ex,"Web order submission failed");TempData["CartError"]="Chưa gửi được yêu cầu. Giỏ hàng vẫn được giữ, bạn vui lòng thử lại.";return RedirectToAction(nameof(Review));}
    }

    private static bool Same(CartQuote a, CartQuote b) => a.Lines.SequenceEqual(b.Lines);
    private sealed record CompletedOrder(int CustomerId,Guid RequestKey,int OrderId);
}
