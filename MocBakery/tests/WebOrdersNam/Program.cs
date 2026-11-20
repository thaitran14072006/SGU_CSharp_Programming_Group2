using MocBakery.BLL.Services.Orders;
using MocBakery.Models.DTOs.Orders;
using MocBakery.Models.WebCart;
using MocBakery.Models.WebOrders;

// Nam Web Orders: BLL contract checks. Fake repository; no SQL data is changed.
var repository = new FakeWebOrders();
var service = new WebOrderService(repository);
var pickup = DateTime.Now.AddDays(2);
var customer = new CustomerOrder(17, DateTime.Now, pickup, 56000m, "MoiTiepNhan", "Hop rieng", null);
repository.Orders[7] = new CustomerOrderDetail(customer,
    [new OrderLine(2, "Banh mi", 2, 28000m)]);
var good = new WebPurchaseRequest(
    7, Guid.NewGuid(), [new CartItem(2, 2)], 56000m,
    pickup, "  Hop rieng  ", [new CartLine(2, "Banh mi", 2, 28000m, true)]);

int count = 0;
void Check(string name, bool ok)
{
    if (!ok) throw new Exception($"FAILED: {name}");
    Console.WriteLine($"PASS: {name}");
    count++;
}

OrderValidationChecks.Run(service, repository, good, Check);
OrderOwnershipChecks.Run(service, Check);
Console.WriteLine($"TOTAL PASS: {count}");
