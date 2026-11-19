namespace MocBakery.Models.DTOs.Orders;
public sealed record OrderLine(int ProductId,string Product,int Quantity,decimal Price)
{
 public decimal Total => Quantity*Price;
}
public sealed record OrderSummary(int Id,int CustomerId,string Customer,int? AccountId,DateTime Placed,DateTime Pickup,decimal Total,decimal Deposit,string Status,string? Note,string? CancelReason)
{
 public decimal Due => Total-Deposit;
 public string Receiver => AccountId is int id ? $"TK{id:000}" : "Chờ tiếp nhận";
}
public interface IOrderRepository
{
 IReadOnlyList<OrderSummary> List();
 IReadOnlyList<OrderLine> Lines(int orderId);
 int Create(int customerId,int accountId,DateTime pickup,decimal deposit,string? note,IReadOnlyList<OrderLine> lines);
 void Update(int orderId,int customerId,DateTime pickup,decimal deposit,string? note,IReadOnlyList<OrderLine> lines);
 void Prepare(int orderId,int accountId);
 void Cancel(int orderId,string reason);
 int Deliver(int orderId,int accountId,Func<IEnumerable<MocBakery.Models.Entities.LoBanh>,int,IReadOnlyList<MocBakery.Models.DTOs.Sales.LotAllocation>> allocate);
}
