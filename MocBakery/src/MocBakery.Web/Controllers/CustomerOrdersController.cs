using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MocBakery.BLL.Services.Orders;
using MocBakery.Web.Security;
namespace MocBakery.Web.Controllers;

[Authorize, Route("don-hang"), ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class CustomerOrdersController(WebOrderService orders) : Controller
{
    [HttpGet("")]
    public IActionResult Index()=>View(orders.List(CustomerCookie.Identity(User)!.CustomerId));
    [HttpGet("{id:int}")]
    public IActionResult Detail(int id)=>orders.Find(CustomerCookie.Identity(User)!.CustomerId,id) is {} order?View(order):NotFound();
}
