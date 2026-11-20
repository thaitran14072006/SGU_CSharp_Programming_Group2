using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Inventory;
using MocBakery.Models.Entities;
using MocBakery.Web.Admin;
using MocBakery.Web.Models.Admin;
using MocBakery.Web.Security;
namespace MocBakery.Web.Controllers;

[Route("admin"),Authorize(AuthenticationSchemes=EmployeeCookie.Scheme),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[RequestSizeLimit(1048576),RequestFormLimits(ValueCountLimit=2000)]
public sealed class WarehouseController(AdminServices services):Controller
{
    private const string Managers=Roles.ChuTiem+","+Roles.Kho;
    private UserSession actor=null!;
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        actor=services.Require(EmployeeCookie.Token(User));
        if(services.Auth.NeedsPasswordChange(actor)){context.Result=Redirect("/admin/doi-mat-khau");return;}
        ViewData["AdminEmployee"]=actor;ViewData["Title"]="Quản lý kho";
    }
    private void Invalid(BusinessException ex)=>ModelState.AddModelError("",ex.Message);
    private void Notice(string message)=>TempData["AdminSuccess"]=message;
    private bool ValidKind(string kind)=>kind is ItemKinds.Cake or ItemKinds.Material;
    [HttpGet("nha-cung-cap")]
    public IActionResult Suppliers()=>View(services.Warehouse.Suppliers(actor));
    [HttpGet("nha-cung-cap/them"),HttpGet("nha-cung-cap/{id:int}/sua"),Authorize(Roles=Managers)]
    public IActionResult Supplier(int id=0)
    {
        var row=services.Warehouse.Suppliers(actor).FirstOrDefault(x=>x.Id==id);if(id>0&&row is null)return NotFound();
        return View(row is null?new SupplierForm():new SupplierForm{Id=row.Id,Name=row.Name,Phone=row.Phone,Address=row.Address,Email=row.Email,Active=row.Active});
    }
    [HttpPost("nha-cung-cap/luu"),Authorize(Roles=Managers)]
    public IActionResult SaveSupplier(SupplierForm input)
    {
        if(input.Id<0||input.Id>0&&!services.Warehouse.Suppliers(actor).Any(x=>x.Id==input.Id))return NotFound();
        if(ModelState.IsValid)try{services.Warehouse.SaveSupplier(actor,new(input.Id,input.Name,input.Phone,input.Address,input.Email,input.Active));Notice("Đã lưu nhà cung cấp.");return RedirectToAction(nameof(Suppliers));}catch(BusinessException ex){Invalid(ex);}
        return View("Supplier",input);
    }
    [HttpGet("nguyen-lieu")]
    public IActionResult Materials()=>View(services.Warehouse.Materials(actor));
    [HttpGet("nguyen-lieu/them"),HttpGet("nguyen-lieu/{id:int}/sua"),Authorize(Roles=Managers)]
    public IActionResult Material(int id=0)
    {
        var row=services.Warehouse.Materials(actor).FirstOrDefault(x=>x.Id==id);if(id>0&&row is null)return NotFound();
        return View(row is null?new MaterialForm():new MaterialForm{Id=row.Id,Name=row.Name,Unit=row.Unit,LowStock=row.LowStock,Active=row.Active});
    }
    [HttpPost("nguyen-lieu/luu"),Authorize(Roles=Managers)]
    public IActionResult SaveMaterial(MaterialForm input)
    {
        if(input.Id<0||input.Id>0&&!services.Warehouse.Materials(actor).Any(x=>x.Id==input.Id))return NotFound();
        if(ModelState.IsValid)try{services.Warehouse.SaveMaterial(actor,new(input.Id,input.Name,input.Unit,input.Active,input.LowStock));Notice("Đã lưu nguyên liệu.");return RedirectToAction(nameof(Materials));}catch(BusinessException ex){Invalid(ex);}
        return View("Material",input);
    }
    [HttpGet("phieu-nhap")]
    public IActionResult Receipts(string? q=null,string? state=null)
    {ViewData["Query"]=q;ViewData["State"]=state;return View(services.Warehouse.Receipts(actor).Where(r=>(string.IsNullOrWhiteSpace(q)||r.Code.Contains(q.Trim(),StringComparison.OrdinalIgnoreCase)||r.Supplier.Contains(q.Trim(),StringComparison.CurrentCultureIgnoreCase))&&(string.IsNullOrEmpty(state)||r.State==state)).ToList());}
    private void ReceiptChoices(){ViewData["Suppliers"]=services.Warehouse.Suppliers(actor);ViewData["Items"]=services.Warehouse.Items(actor);}
    [HttpGet("phieu-nhap/them"),Authorize(Roles=Managers)]
    public IActionResult NewReceipt(){ReceiptChoices();return View("Receipt",new ReceiptForm());}
    [HttpGet("phieu-nhap/{id:int}")]
    public IActionResult Receipt(int id)
    {if(!services.Warehouse.Receipts(actor).Any(x=>x.Id==id))return NotFound();ReceiptChoices();return View(ReceiptForm.From(services.Warehouse.Receipt(actor,id)));}
    [HttpPost("phieu-nhap/luu"),Authorize(Roles=Managers)]
    public IActionResult SaveReceipt(ReceiptForm input)
    {
        if(input.Id<0||input.Id>0&&!services.Warehouse.Receipts(actor).Any(x=>x.Id==input.Id))return NotFound();
        input.State=input.Id==0?"Nhap":services.Warehouse.Receipt(actor,input.Id).State;
        if(ModelState.IsValid)try
        {
            var items=services.Warehouse.Items(actor).ToDictionary(x=>x.Kind+":"+x.Id);
            var lines=input.Lines.Select(l=>
            {
                if(!items.TryGetValue(l.Item,out var item))throw new BusinessException("Chọn mặt hàng hợp lệ.");
                return new ReceiptLine(item.Kind,item.Id,l.LotCode,l.Produced,l.Expiry,l.Quantity,item.Unit,l.Cost);
            }).ToList();
            byte[]? version=null;
            if(input.Id>0){if(string.IsNullOrEmpty(input.Version))throw new BusinessException("Phiếu thiếu phiên bản. Tải lại trước khi lưu.");try{version=Convert.FromBase64String(input.Version);}catch(FormatException){throw new BusinessException("Phiên bản phiếu không hợp lệ. Tải lại trước khi lưu.");}}
            int id=services.Warehouse.SaveDraft(actor,new(input.Id,input.SupplierId,input.Date,"Nhap",lines,version));Notice("Đã lưu nháp. Tồn kho chưa thay đổi.");return RedirectToAction(nameof(Receipt),new{id});
        }catch(BusinessException ex){Invalid(ex);}
        if(input.Lines.Count==0)input.Lines.Add(new());ReceiptChoices();return View("Receipt",input);
    }
    [HttpPost("phieu-nhap/{id:int}/ghi-so"),Authorize(Roles=Managers)]
    public IActionResult PostReceipt(int id)
    {
        if(!services.Warehouse.Receipts(actor).Any(x=>x.Id==id))return NotFound();
        try{services.Warehouse.Post(actor,id);Notice("Đã ghi sổ phiếu, tạo các lô và nhật ký kho.");}catch(BusinessException ex){TempData["AdminError"]=ex.Message;}
        return RedirectToAction(nameof(Receipt),new{id});
    }
    [HttpGet("ton-kho")]
    public IActionResult Stock(string kind=ItemKinds.Cake,string? q=null,string filter="All")
    {
        if(!ValidKind(kind)||filter is not("All" or "Low" or "Soon" or "Expired" or "Stopped"))return BadRequest();
        return View(new WarehouseStockPage(kind,q??"",filter,services.Warehouse.Stock(actor,kind,q??"",filter)));
    }
    [HttpGet("ton-kho/{kind}/{id:int}/lich-su")]
    public IActionResult History(string kind,int id)
    {
        if(!ValidKind(kind))return BadRequest();var lot=services.Warehouse.Stock(actor,kind).FirstOrDefault(x=>x.Id==id);if(lot is null)return NotFound();
        return View(new WarehouseHistoryPage(lot,services.Warehouse.History(actor,kind,id)));
    }
    private void StockChoices()=>ViewData["StockItems"]=services.Warehouse.Items(actor);
    [HttpGet("ton-kho/dieu-chinh"),Authorize(Roles=Managers)]
    public IActionResult Change(string kind=ItemKinds.Cake,int lotId=0)
    {
        if(!ValidKind(kind))return BadRequest();StockChoices();
        var lot=services.Warehouse.Stock(actor,kind).FirstOrDefault(x=>x.Id==lotId);if(lotId>0&&lot is null)return NotFound();
        ViewData["Lot"]=lot;return View(new StockChangeForm{Kind=kind,LotId=lotId,ItemId=lot?.ItemId??0,Quantity=lot?.Remaining??0});
    }
    [HttpPost("ton-kho/dieu-chinh"),Authorize(Roles=Managers)]
    public IActionResult Change(StockChangeForm input)
    {
        if(!ValidKind(input.Kind))return BadRequest();
        var lot=input.LotId>0?services.Warehouse.Stock(actor,input.Kind).FirstOrDefault(x=>x.Id==input.LotId):null;
        if(input.LotId>0&&lot is null)return NotFound();if(lot is not null)input.ItemId=lot.ItemId;
        if(!services.Warehouse.Items(actor).Any(x=>x.Kind==input.Kind&&x.Id==input.ItemId))ModelState.AddModelError("ItemId","Chọn mặt hàng hợp lệ.");
        if(ModelState.IsValid)try{string reason=input.Operation=="Xuat"?(input.Disposal?"Hủy: ":"Xuất kho: ")+input.Reason:input.Reason;services.Warehouse.ChangeStock(actor,new(input.Kind,input.LotId,input.ItemId,input.Operation,input.Quantity,reason));Notice("Đã cập nhật tồn kho và ghi nhật ký người thực hiện, số cũ, số mới, lý do.");return input.LotId>0?RedirectToAction(nameof(History),new{kind=input.Kind,id=input.LotId}):RedirectToAction(nameof(Stock),new{kind=input.Kind});}catch(BusinessException ex){Invalid(ex);}
        StockChoices();ViewData["Lot"]=lot;return View(input);
    }
}
