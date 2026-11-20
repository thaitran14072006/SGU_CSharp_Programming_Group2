using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Orders;
using MocBakery.Models.DTOs.Inventory;
using MocBakery.Models.DTOs.Reports;
namespace MocBakery.Web.Models.Admin;

public class AdminLoginForm
{
    [Required(ErrorMessage="Nhập tên đăng nhập."),StringLength(50)] public string Username {get;set;}="";
    [Required(ErrorMessage="Nhập mật khẩu."),StringLength(64)] public string Password {get;set;}="";
    public string? ReturnUrl {get;set;}
}
public sealed class AdminPasswordForm
{
    [Required] public string CurrentPassword {get;set;}="";
    [Required,StringLength(64,MinimumLength=8)] public string Password {get;set;}="";
    [Required,Compare(nameof(Password),ErrorMessage="Mật khẩu xác nhận chưa khớp.")] public string Confirmation {get;set;}="";
}
public sealed record AdminDashboard(decimal? Revenue,int? Invoices,int Pending,long Stock,
    IReadOnlyList<OrderSummary> Orders,IReadOnlyList<StockRow> LowStock,IReadOnlyList<RevenueDay> Days,IReadOnlyList<ProductSales> Bestsellers);
public sealed class AdminProductForm
{
    public int Id {get;set;}
    [Required,StringLength(100)] public string Name {get;set;}="";
    [Range(1,int.MaxValue)] public int CategoryId {get;set;}
    public string Unit {get;set;}="Cái";
    [ModelBinder(BinderType=typeof(HtmlDecimalBinder)),Range(typeof(decimal),"0.01","9999999999999999.99",ParseLimitsInInvariantCulture=true,ConvertValueInInvariantCulture=true)] public decimal Price {get;set;}
    [StringLength(500)] public string? Description {get;set;}
    public bool Active {get;set;}=true;
    [Range(1,3650)] public int ShelfDays {get;set;}=1;
    [ModelBinder(BinderType=typeof(HtmlDecimalBinder)),Range(typeof(decimal),"0","1000000000",ParseLimitsInInvariantCulture=true,ConvertValueInInvariantCulture=true)] public decimal Threshold {get;set;}=5;
    public IFormFile? Picture {get;set;}
    public bool RemovePicture {get;set;}
}
public sealed class AdminCategoryForm
{
    public int Id {get;set;}
    [Required,StringLength(100)] public string Name {get;set;}="";
    [StringLength(255)] public string? Description {get;set;}
    public bool Active {get;set;}=true;
}
public sealed class AdminLotForm
{
    [Range(1,int.MaxValue)] public int ProductId {get;set;}
    [StringLength(100)] public string? Source {get;set;}
    public DateOnly Produced {get;set;}=DateOnly.FromDateTime(DateTime.Today);
    public DateOnly Expiry {get;set;}=DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    [Range(1,1000000)] public int Quantity {get;set;}=1;
}
public sealed class AdminCustomerForm
{
    public int Id {get;set;}
    [Required,StringLength(100)] public string FullName {get;set;}="";
    [Required,RegularExpression(@"[0-9]{10}")] public string Phone {get;set;}="";
    [StringLength(255)] public string? Address {get;set;}
    [StringLength(255)] public string? Note {get;set;}
}
public sealed class AdminStaffForm
{
    public int Id {get;set;}
    [Required,StringLength(100)] public string FullName {get;set;}="";
    [Required,RegularExpression(@"[0-9]{10}")] public string Phone {get;set;}="";
    public string Role {get;set;}=Roles.NhanVien;
    public DateOnly StartDate {get;set;}=DateOnly.FromDateTime(DateTime.Today);
    public bool Active {get;set;}=true;
}
public sealed class AdminAccountForm
{
    public int Id {get;set;}
    [Required,StringLength(50,MinimumLength=3)] public string Username {get;set;}="";
    [Required,StringLength(100)] public string FullName {get;set;}="";
    public string Role {get;set;}=Roles.NhanVien;
    public bool Active {get;set;}=true;
    public string Password {get;set;}="";
    public int? EmployeeId {get;set;}
}
public sealed record AdminOrderDetail(OrderSummary Order,IReadOnlyList<OrderLine> Lines);
public sealed record AdminInventory(IReadOnlyList<StockRow> Stock,IReadOnlyList<LotRow> Lots);
public static class AdminDisplay
{
    public static string Status(string value)=>value switch{"MoiTiepNhan"=>"Mới tiếp nhận","DangChuanBi"=>"Đang chuẩn bị","DaGiao"=>"Đã giao","DaHuy"=>"Đã hủy",_=>value};
    public static string? Photo(SanPham p)=>p.AnhBanh is {Length:>0}?$"/admin/banh/{p.MaSanPham}/anh":BakeryDisplay.Photo(new(p.MaSanPham,p.MaLoai,"",p.TenSanPham,"",p.DonViTinh,p.GiaBan,p.MoTa,false,false));
}
