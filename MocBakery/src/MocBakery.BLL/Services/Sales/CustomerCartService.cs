using MocBakery.Models.WebCart;
namespace MocBakery.BLL.Services.Sales;

public sealed class CustomerCartService(ICustomerCartRepository repository,CartService cart)
{
    public SavedCart Read(int customerId)=>repository.Read(customerId);
    public SavedCart Change(int customerId,int productId,int quantity,bool add)=>
        repository.Update(customerId,items=>cart.Change(items,productId,quantity,add));
    public void MergeGuest(int customerId,IReadOnlyList<CartItem> guest)=>repository.Update(customerId,items=>
        items.Concat(guest).GroupBy(x=>x.ProductId).Select(g=>new CartItem(g.Key,(int)Math.Min(1000L,g.Sum(x=>(long)x.Quantity)))).Take(50).OrderBy(x=>x.ProductId).ToList());
    public bool ClearIfVersion(int customerId,long version)=>repository.ClearIfVersion(customerId,version);
}
