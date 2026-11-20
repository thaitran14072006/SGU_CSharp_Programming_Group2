using MocBakery.Models.WebCart;
using MocBakery.Models.WebOrders;

// Deterministic in-memory fake for isolated BLL checks (not an SQL integration test).
internal sealed class FakeWebOrders : IWebOrderRepository
{
    public Dictionary<int, CustomerOrderDetail> Orders { get; } = new();
    public int SubmitCalls { get; private set; }
    public WebPurchaseRequest? LastRequest { get; private set; }

    public int Submit(WebPurchaseRequest request)
    {
        SubmitCalls++;
        LastRequest = request;
        return 101;
    }

    public IReadOnlyList<CustomerOrder> List(int customerId) =>
        customerId == 7 ? Orders.Values.Select(x => x.Order).ToArray() : [];

    public CustomerOrderDetail? Find(int customerId, int orderId) =>
        customerId == 7 && Orders.TryGetValue(orderId, out var order) ? order : null;
}
