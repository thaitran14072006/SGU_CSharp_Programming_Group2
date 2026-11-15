using MocBakery.Models.DTOs.Customers;
using MocBakery.Models.Entities;

namespace MocBakery.Models.DTOs.Sales;

public sealed record SalesProduct(int Id, string Name, decimal Price, long Stock)
{
    public string Category { get; init; } = "";
}
public sealed record CartItem(int ProductId, int Quantity, decimal UnitPrice);
public sealed record LotAllocation(int LotId, int Quantity);
public sealed record InvoiceLine(int LotId, string Product, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}

public interface ISalesRepository
{
    IReadOnlyList<SalesProduct> ListProducts();
    IReadOnlyList<InvoiceSummary> ListInvoices();
    IReadOnlyList<InvoiceLine> ReadInvoice(int invoiceId);
    int Sell(int accountId, int? customerId, IReadOnlyList<CartItem> cart,
        Func<IEnumerable<LoBanh>, int, IReadOnlyList<LotAllocation>> allocate);
}
