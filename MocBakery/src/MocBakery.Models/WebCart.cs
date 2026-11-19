namespace MocBakery.Models.WebCart;

public sealed record CartItem(int ProductId, int Quantity);
public sealed record CartLine(int ProductId, string Name, int Quantity, decimal UnitPrice, bool Available)
{
    public decimal Total => Quantity * UnitPrice;
}
public sealed record CartQuote(IReadOnlyList<CartLine> Lines)
{
    public decimal Total => Lines.Sum(x => x.Total);
    public bool CanSubmit => Lines.Count > 0 && Lines.All(x => x.Available);
}

// Nam implements this boundary. Creating a request must not invoice or deduct stock.
// RequestKey must be unique per customer in SQL; replay returns the same order id.
public sealed record WebPurchaseRequest(int CustomerId, Guid RequestKey, IReadOnlyList<CartItem> Items, decimal ConfirmedTotal,
    DateTime? Pickup = null, string? Note = null, IReadOnlyList<CartLine>? ConfirmedLines = null);
public interface IWebOrderSubmission
{
    int Submit(WebPurchaseRequest request);
}
