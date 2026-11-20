using MocBakery.BLL.Services.Orders;

internal static class OrderOwnershipChecks
{
    internal static void Run(WebOrderService service, Action<string, bool> check)
    {
        check("Customer sees own list", service.List(7).Count == 1);
        check("Customer sees own detail", service.Find(7, 17)?.Order.Id == 17);
        check("Another customer cannot read detail", service.Find(8, 17) is null);
        check("Another customer cannot list orders", service.List(8).Count == 0);
        check("Invalid customer cannot list", service.List(0).Count == 0);
        check("Invalid order cannot be found", service.Find(7, 0) is null);
    }
}
