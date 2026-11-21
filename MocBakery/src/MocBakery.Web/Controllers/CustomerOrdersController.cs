using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MocBakery.BLL.Services.Orders;
using MocBakery.Web.Security;
using MocBakery.Models;
namespace MocBakery.Web.Controllers;

[Authorize, Route("don-hang"), ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class CustomerOrdersController(WebOrderService orders) : Controller
{
    [HttpGet("")]
    public IActionResult Index()=>View(orders.List(CustomerCookie.Identity(User)!.CustomerId));
    [HttpGet("{id:int}")]
    public IActionResult Detail(int id)=>orders.Find(CustomerCookie.Identity(User)!.CustomerId,id) is {} order?View(order):NotFound();
    [HttpPost("{id:int}/huy"), RequestSizeLimit(8192)]
    public IActionResult Cancel(int id,string? reason)
    {
        int customer=CustomerCookie.Identity(User)!.CustomerId;
        if(orders.Find(customer,id) is null)return NotFound();
        try
        {
            if(!ModelState.IsValid)throw new BusinessException("Lý do hủy không hợp lệ.");
            orders.Cancel(customer,id,reason);
            TempData["OrderMessage"]="Đã hủy đơn hàng.";
        }
        catch(BusinessException ex){TempData["OrderError"]=ex.Message;}
        return RedirectToAction(nameof(Detail),new{id});
    }
}
