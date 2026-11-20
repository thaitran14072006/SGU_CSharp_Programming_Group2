using MocBakery.BLL.Services.Orders;
using MocBakery.Models;
using MocBakery.Models.WebCart;

internal static class OrderValidationChecks
{
    internal static void Run(WebOrderService service, FakeWebOrders repository,
        WebPurchaseRequest good, Action<string, bool> check)
    {
        void Reject(string name, WebPurchaseRequest request)
        {
            int before = repository.SubmitCalls;
            try
            {
                service.Submit(request);
                throw new Exception($"FAILED: {name} (accepted invalid request)");
            }
            catch (BusinessException)
            {
                check(name, repository.SubmitCalls == before);
            }
        }

        check("Valid web order delegates to repository", service.Submit(good) == 101);
        check("Notes are trimmed at BLL boundary", repository.LastRequest?.Note == "Hop rieng");
        Reject("Missing customer", good with { CustomerId = 0 });
        Reject("Missing request key", good with { RequestKey = Guid.Empty });
        Reject("Empty cart", good with { Items = [] });
        Reject("Invalid product", good with { Items = [new CartItem(0, 2)] });
        Reject("Zero quantity", good with { Items = [new CartItem(2, 0)] });
        Reject("Quantity exceeds limit", good with { Items = [new CartItem(2, 1001)] });
        Reject("Duplicate products", good with { Items = [new CartItem(2, 1), new CartItem(2, 1)] });
        Reject("Invalid total", good with { ConfirmedTotal = 0 });
        Reject("Missing server-confirmed prices", good with { ConfirmedLines = null });
        Reject("Mismatched confirmed quantity", good with { ConfirmedLines = [new CartLine(2, "Banh mi", 1, 28000m, true)] });
        Reject("Zero confirmed price", good with { ConfirmedLines = [new CartLine(2, "Banh mi", 2, 0m, true)] });
        Reject("Missing pickup", good with { Pickup = null });
        Reject("Overlong note", good with { Note = new string('x', 501) });
    }
}
