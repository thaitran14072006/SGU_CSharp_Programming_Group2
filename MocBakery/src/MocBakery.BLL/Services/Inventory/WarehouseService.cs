using System.Net.Mail;
using System.Text.RegularExpressions;
using MocBakery.BLL.Services.Accounts;
using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Inventory;
using MocBakery.Models.Entities;
namespace MocBakery.BLL.Services.Inventory;
public sealed class WarehouseService(IWarehouseRepository repository,AuthService auth,Func<DateTime>? clock=null)
{
    private DateTime Now=>clock?.Invoke()??DateTime.Now;
    private int Manage(UserSession session)
    {
        var actor=auth.Require(session);
        if(!Roles.CanManageInventory(actor.VaiTro))throw new BusinessException("Chỉ quản lý hoặc nhân viên kho được thay đổi kho.");
        return actor.MaTaiKhoan;
    }
    public IReadOnlyList<WarehouseItem> Items(UserSession session){auth.Require(session);return repository.Items();}
    public IReadOnlyList<Supplier> Suppliers(UserSession session){auth.Require(session);return repository.Suppliers();}
    public int SaveSupplier(UserSession session,Supplier value)
    {
        Manage(session);string name=Clean(value.Name,150,"Tên nhà cung cấp","Name");
        if(!Regex.IsMatch(value.Phone.Trim(),@"^[0-9]{10,11}$"))throw new BusinessException("Điện thoại phải có 10–11 chữ số.","Phone");
        string? email=string.IsNullOrWhiteSpace(value.Email)?null:value.Email.Trim();
        if(email is not null&&(!MailAddress.TryCreate(email,out var parsed)||parsed.Address!=email||email.Length>255))throw new BusinessException("Email không hợp lệ.","Email");
        if(value.Address?.Length>255)throw new BusinessException("Địa chỉ tối đa 255 ký tự.","Address");
        return repository.SaveSupplier(value with{Name=name,Phone=value.Phone.Trim(),Address=value.Address?.Trim(),Email=email});
    }
    public IReadOnlyList<Material> Materials(UserSession session){auth.Require(session);return repository.Materials();}
    public int SaveMaterial(UserSession session,Material value)
    {
        Manage(session);string name=Clean(value.Name,150,"Tên nguyên liệu","Name"),unit=Clean(value.Unit,20,"Đơn vị","Unit");
        if(value.LowStock<0||value.LowStock>1000000000||decimal.Round(value.LowStock,3)!=value.LowStock)throw new BusinessException("Ngưỡng tồn không hợp lệ.","Threshold");
        return repository.SaveMaterial(value with{Name=name,Unit=unit});
    }
    public IReadOnlyList<ReceiptSummary> Receipts(UserSession session){auth.Require(session);return repository.Receipts();}
    public Receipt Receipt(UserSession session,int id){auth.Require(session);return repository.GetReceipt(id);}
    public int SaveDraft(UserSession session,Receipt input)
    {
        int actor=Manage(session);
        if(input.Date>Now||input.Date.Year<2000)throw new BusinessException("Ngày nhập không được sau hiện tại.","Date");
        if(input.Lines.Count==0)throw new BusinessException("Phiếu phải có ít nhất một dòng hàng.");
        if(input.Lines.Count>200)throw new BusinessException("Mỗi phiếu tối đa 200 dòng hàng.");
        if(!repository.Suppliers().Any(s=>s.Id==input.SupplierId&&s.Active))throw new BusinessException("Chọn nhà cung cấp đang hợp tác.","Supplier");
        var items=repository.Items().ToDictionary(i=>(i.Kind,i.Id));var date=DateOnly.FromDateTime(input.Date);
        foreach(var line in input.Lines)
        {
            if(!items.TryGetValue((line.Kind,line.ItemId),out var item)||!item.Active)throw new BusinessException("Mặt hàng đã ngừng sử dụng.","Item");
            if(line.Unit!=item.Unit)throw new BusinessException("Đơn vị mặt hàng đã đổi. Hãy tải lại.","Unit");
            if(line.Quantity<=0||line.Quantity>1000000000||decimal.Round(line.Quantity,3)!=line.Quantity||(line.Kind==ItemKinds.Cake&&decimal.Truncate(line.Quantity)!=line.Quantity))
                throw new BusinessException("Số lượng phải dương; bánh dùng số nguyên, nguyên liệu tối đa 3 số lẻ.","Quantity");
            if(line.Cost<0||line.Cost>1000000000||decimal.Round(line.Cost,2)!=line.Cost||line.Amount>9999999999999999.99m)
                throw new BusinessException("Đơn giá không âm, tối đa 2 số lẻ; thành tiền không được vượt giới hạn.","Cost");
            Clean(line.LotCode,50,"Số lô","Lot");
            if(line.Produced>date||line.Expiry<date)throw new BusinessException("Phải có ngày sản xuất ≤ ngày nhập ≤ hạn dùng.","Expiry");
        }
        if(input.Total>9999999999999999.99m)throw new BusinessException("Tổng tiền vượt giới hạn.");
        if(input.Lines.GroupBy(l=>(l.Kind,l.ItemId,l.LotCode.Trim().ToUpperInvariant())).Any(g=>g.Count()>1))throw new BusinessException("Không nhập trùng số lô cho cùng mặt hàng trong phiếu.","Lot");
        return repository.SaveDraft(actor,input with{Lines=input.Lines.Select(l=>l with{LotCode=l.LotCode.Trim()}).ToList()});
    }
    public void Post(UserSession session,int id){int actor=Manage(session);if(id<=0)throw new BusinessException("Lưu nháp phiếu trước.");repository.Post(actor,id);}
    public void ChangeStock(UserSession session,StockChange input)
    {
        int actor=Manage(session);
        if(input.Kind is not(ItemKinds.Cake or ItemKinds.Material)||input.Operation is not("Xuat" or "KiemKe"))throw new BusinessException("Thao tác kho không hợp lệ.");
        if(input.Quantity<0||input.Quantity>1000000000||decimal.Round(input.Quantity,3)!=input.Quantity||(input.Operation=="Xuat"&&input.Quantity==0)||(input.Kind==ItemKinds.Cake&&decimal.Truncate(input.Quantity)!=input.Quantity))
            throw new BusinessException("Số lượng không hợp lệ.","Quantity");
        string reason=Clean(input.Reason,500,"Lý do","Reason");
        if(input.Operation=="KiemKe"&&input.LotId<=0)throw new BusinessException("Chọn lô để kiểm kê.");
        repository.ChangeStock(actor,input with{Reason=reason});
    }
    public IReadOnlyList<WarehouseRow> Stock(UserSession session,string kind,string keyword="",string filter="All")
    {
        auth.Require(session);var today=DateOnly.FromDateTime(Now);var lots=repository.Lots().Where(l=>l.Kind==kind).ToList();
        var available=lots.Where(l=>l.Active&&l.State=="DangBan"&&l.Produced<=today&&l.Expiry>=today).GroupBy(l=>l.ItemId).ToDictionary(g=>g.Key,g=>g.Sum(l=>l.Remaining));
        return lots.Where(l=>l.Name.Contains(keyword.Trim(),StringComparison.CurrentCultureIgnoreCase)||l.Code.Contains(keyword.Trim(),StringComparison.OrdinalIgnoreCase))
            .Select(l=>{
                bool usable=l.Active&&l.State=="DangBan"&&l.Remaining>0&&l.Produced<=today&&l.Expiry>=today;
                bool low=available.GetValueOrDefault(l.ItemId)<l.LowStock;
                string warning=l.Expiry<today?"Hết hạn":!l.Active||l.State!="DangBan"?"Ngừng sử dụng":l.Remaining==0?"Hết hàng":l.Expiry<=today.AddDays(3)?"Sắp hết hạn":low?"Sắp hết":"Bình thường";
                return (Row:new WarehouseRow(l.Kind,l.Id,l.ItemId,l.Code,l.Name,l.Unit,l.Expiry,l.Remaining,warning,l.State,usable),Low:low);
            }).Where(x=>filter switch{"Low"=>x.Low,"Soon"=>x.Row.Expiry>=today&&x.Row.Expiry<=today.AddDays(3)&&x.Row.Remaining>0,"Expired"=>x.Row.Expiry<today,"Stopped"=>x.Row.State!="DangBan",_=>true})
            .Select(x=>x.Row).OrderBy(l=>l.Expiry).ThenBy(l=>l.Id).ToList();
    }
    public IReadOnlyList<StockHistory> History(UserSession session,string kind,int lotId){auth.Require(session);return repository.History(kind,lotId);}
    private static string Clean(string value,int max,string label,string field)
    {
        string result=Regex.Replace(value.Trim(),@"\s+"," ");if(result.Length<1||result.Length>max)throw new BusinessException($"{label} phải có 1–{max} ký tự.",field);return result;
    }
}
