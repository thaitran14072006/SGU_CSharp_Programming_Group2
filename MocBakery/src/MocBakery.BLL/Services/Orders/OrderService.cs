using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.Sales;
using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Orders;
namespace MocBakery.BLL.Services.Orders;
public sealed class OrderService(IOrderRepository repository,AuthService auth)
{
 public IReadOnlyList<OrderSummary> List(UserSession s){auth.RequireSales(s);return repository.List();}
 public IReadOnlyList<OrderLine> Lines(UserSession s,int id){auth.RequireSales(s);return repository.Lines(id);}
 public int Create(UserSession s,int customerId,DateTime pickup,decimal deposit,string? note,IReadOnlyList<OrderLine> lines){int actor=auth.RequireSales(s).MaTaiKhoan;Validate(customerId,deposit,note,lines);return repository.Create(customerId,actor,pickup,deposit,note,lines);}
 public void Update(UserSession s,int id,int customerId,DateTime pickup,decimal deposit,string? note,IReadOnlyList<OrderLine> lines){auth.RequireSales(s);Validate(customerId,deposit,note,lines);repository.Update(id,customerId,pickup,deposit,note,lines);}
 public void Prepare(UserSession s,int id){auth.RequireSales(s);repository.Prepare(id);}
 public void Cancel(UserSession s,int id,string reason){auth.RequireSales(s);if(string.IsNullOrWhiteSpace(reason)||reason.Trim().Length>255)throw new BusinessException("Hủy đơn phải có lý do (tối đa 255 ký tự).");repository.Cancel(id,reason.Trim());}
 public int Deliver(UserSession s,int id){int actor=auth.RequireSales(s).MaTaiKhoan;return repository.Deliver(id,actor,SalesService.Allocate);}
 public static void Validate(int customerId,decimal deposit,string? note,IReadOnlyList<OrderLine> lines)
 {
  if(customerId<=0||lines.Count==0||lines.Any(x=>x.ProductId<=0||x.Quantity<=0||x.Price<0)||lines.Select(x=>x.ProductId).Distinct().Count()!=lines.Count)throw new BusinessException("Đơn phải có khách hàng và ít nhất một sản phẩm hợp lệ.");
  if(note?.Length>500)throw new BusinessException("Yêu cầu tối đa 500 ký tự.");
  decimal total;try{total=lines.Sum(x=>checked(x.Quantity*x.Price));}catch(OverflowException){throw new BusinessException("Giá trị đơn vượt giới hạn.");}
  if(deposit<0||deposit>total)throw new BusinessException("Tiền cọc phải từ 0 đến tổng tiền.");
 }
}
