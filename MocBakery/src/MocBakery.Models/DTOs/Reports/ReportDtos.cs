namespace MocBakery.Models.DTOs.Reports;
public sealed record RevenueDay(DateTime Day,int InvoiceCount,decimal Revenue);
public sealed record ProductSales(int ProductId,string Name,long Quantity,decimal Revenue);
public sealed record Dashboard(decimal Revenue,int Invoices,int PendingOrders,long AvailableStock);
public sealed record LowStockItem(int ProductId,string Name,long Quantity);
public sealed record UpcomingOrder(int Id,string Customer,DateTime Pickup,string Status);
public interface IReportRepository
{
 IReadOnlyList<RevenueDay> Revenue(DateTime from,DateTime to);
 IReadOnlyList<ProductSales> Bestsellers(DateTime from,DateTime to);
 Dashboard Overview(DateTime from,DateTime to);
 IReadOnlyList<LowStockItem> LowStock(int threshold);
 IReadOnlyList<UpcomingOrder> Upcoming(DateTime day);
}
