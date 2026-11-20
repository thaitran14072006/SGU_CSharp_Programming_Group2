using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MocBakery.Models.DTOs.Inventory;
namespace MocBakery.Web.Models.Admin;

public sealed class SupplierForm
{
    public int Id {get;set;}
    [Required,StringLength(150)] public string Name {get;set;}="";
    [Required,RegularExpression("[0-9]{10,11}")] public string Phone {get;set;}="";
    [StringLength(255)] public string? Address {get;set;}
    [EmailAddress,StringLength(255)] public string? Email {get;set;}
    public bool Active {get;set;}=true;
}
public sealed class MaterialForm
{
    public int Id {get;set;}
    [Required,StringLength(150)] public string Name {get;set;}="";
    [Required,StringLength(20)] public string Unit {get;set;}="Kg";
    [ModelBinder(BinderType=typeof(HtmlDecimalBinder))] public decimal LowStock {get;set;}=5;
    public bool Active {get;set;}=true;
}
public sealed class ReceiptLineForm
{
    [Required] public string Item {get;set;}="";
    [Required,StringLength(50)] public string LotCode {get;set;}="";
    public DateOnly Produced {get;set;}=DateOnly.FromDateTime(DateTime.Today);
    public DateOnly Expiry {get;set;}=DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    [ModelBinder(BinderType=typeof(HtmlDecimalBinder))] public decimal Quantity {get;set;}=1;
    [ModelBinder(BinderType=typeof(HtmlDecimalBinder))] public decimal Cost {get;set;}
}
public sealed class ReceiptForm
{
    public int Id {get;set;}
    [Range(1,int.MaxValue)] public int SupplierId {get;set;}
    public DateTime Date {get;set;}=DateTime.Today;
    public string? Version {get;set;}
    public string State {get;set;}="Nhap";
    [MaxLength(200)] public List<ReceiptLineForm> Lines {get;set;}=[new()];
    public static ReceiptForm From(Receipt r)=>new(){Id=r.Id,SupplierId=r.SupplierId,Date=r.Date,Version=r.Version is null?"":Convert.ToBase64String(r.Version),State=r.State,
        Lines=r.Lines.Select(l=>new ReceiptLineForm{Item=l.Kind+":"+l.ItemId,LotCode=l.LotCode,Produced=l.Produced,Expiry=l.Expiry,Quantity=l.Quantity,Cost=l.Cost}).ToList()};
}
public sealed class StockChangeForm
{
    public string Kind {get;set;}=ItemKinds.Cake;
    public int LotId {get;set;}
    public int ItemId {get;set;}
    public string Operation {get;set;}="KiemKe";
    public bool Disposal {get;set;}
    [ModelBinder(BinderType=typeof(HtmlDecimalBinder))] public decimal Quantity {get;set;}
    [Required,StringLength(490)] public string Reason {get;set;}="";
}
public sealed record WarehouseStockPage(string Kind,string Query,string Filter,IReadOnlyList<WarehouseRow> Rows);
public sealed record WarehouseHistoryPage(WarehouseRow Lot,IReadOnlyList<StockHistory> History);
