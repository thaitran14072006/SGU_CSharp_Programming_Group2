using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Orders;
using MocBakery.Web.Admin;
using MocBakery.Web.Models.Admin;
using MocBakery.Web.Security;
namespace MocBakery.Web.Controllers;

[Route("admin"),Authorize(AuthenticationSchemes=EmployeeCookie.Scheme),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class AdminController(AdminServices services) : Controller
{
    private const string SalesRoles=Roles.ChuTiem+","+Roles.ThuNgan+","+Roles.NhanVien;
    private UserSession employee=null!;
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        employee=services.Require(EmployeeCookie.Token(User));
        if(services.Auth.NeedsPasswordChange(employee)){context.Result=Redirect("/admin/doi-mat-khau");return;}
        ViewData["AdminEmployee"]=employee;ViewData["Title"]="Quản trị";
    }
    private void Success(string text)=>TempData["AdminSuccess"]=text;
    private void Error(BusinessException ex)=>TempData["AdminError"]=ex.Message;
    private void Invalid(BusinessException ex)=>ModelState.AddModelError("",ex.Message);

    [HttpGet("")]
    public IActionResult Dashboard()
    {
        var stock=services.Inventory.Stock(employee);var orders=Roles.CanSell(employee.VaiTro)?services.Orders.List(employee):[];
        bool owner=employee.VaiTro==Roles.ChuTiem;
        var overview=owner?services.Reports.Overview(employee,DateTime.Today,DateTime.Today):null;
        return View(new AdminDashboard(overview?.Revenue,overview?.Invoices,orders.Count(x=>x.Status=="MoiTiepNhan"),stock.Sum(x=>x.TonCoTheBan),
            orders.Where(x=>x.Status is "MoiTiepNhan" or "DangChuanBi").OrderBy(x=>x.Pickup).Take(6).ToList(),
            stock.Where(x=>x.DangKinhDoanh&&x.TonCoTheBan<5).OrderBy(x=>x.TonCoTheBan).Take(6).ToList(),
            owner?services.Reports.Revenue(employee,DateTime.Today.AddDays(-6),DateTime.Today):[],
            owner?services.Reports.Bestsellers(employee,DateTime.Today.AddDays(-29),DateTime.Today).Take(5).ToList():[]));
    }

    [HttpGet("don-hang"),Authorize(Roles=SalesRoles)]
    public IActionResult Orders(string? status=null)
    {ViewData["Status"]=status;return View(services.Orders.List(employee).Where(x=>string.IsNullOrEmpty(status)||x.Status==status).OrderByDescending(x=>x.Placed).ToList());}
    [HttpGet("don-hang/{id:int}"),Authorize(Roles=SalesRoles)]
    public IActionResult Order(int id)=>services.Orders.List(employee).FirstOrDefault(x=>x.Id==id) is {} order?View(new AdminOrderDetail(order,services.Orders.Lines(employee,id))):NotFound();
    [HttpPost("don-hang/{id:int}/tiep-nhan"),Authorize(Roles=SalesRoles)]
    public IActionResult Accept(int id)
    {try{services.Orders.Prepare(employee,id);Success("Đã tiếp nhận đơn và chuyển sang chuẩn bị.");}catch(BusinessException ex){Error(ex);}return RedirectToAction(nameof(Order),new{id});}
    [HttpPost("don-hang/{id:int}/giao"),Authorize(Roles=SalesRoles)]
    public IActionResult Deliver(int id)
    {try{int invoice=services.Orders.Deliver(employee,id);Success("Đã giao đơn, lập hóa đơn và cập nhật tồn kho.");return RedirectToAction(nameof(Invoice),new{id=invoice});}catch(BusinessException ex){Error(ex);return RedirectToAction(nameof(Order),new{id});}}
    [HttpPost("don-hang/{id:int}/huy"),Authorize(Roles=SalesRoles)]
    public IActionResult Cancel(int id,string? reason)
    {try{services.Orders.Cancel(employee,id,reason??"");Success("Đã hủy đơn. Tồn kho được giữ nguyên.");}catch(BusinessException ex){Error(ex);}return RedirectToAction(nameof(Order),new{id});}

    [HttpGet("banh")]
    public IActionResult Products(string? q=null)=>View(services.Catalog.Products(employee,q??""));
    [HttpGet("banh/them"),HttpGet("banh/{id:int}/sua"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult Product(int id=0)
    {
        var p=id>0?services.Catalog.Products(employee).FirstOrDefault(x=>x.MaSanPham==id):null;
        if(id>0&&p is null)return NotFound();CategoriesData();
        return View(p is null?new AdminProductForm():new AdminProductForm{Id=p.MaSanPham,CategoryId=p.MaLoai,Name=p.TenSanPham,Unit=p.DonViTinh,Price=p.GiaBan,Description=p.MoTa,Active=p.DangKinhDoanh,ShelfDays=p.HanDungNgay,Threshold=p.NguongCanhBao});
    }
    [HttpPost("banh/luu"),Authorize(Roles=Roles.ChuTiem),RequestSizeLimit(6291456),RequestFormLimits(MultipartBodyLengthLimit=5242880)]
    public async Task<IActionResult> SaveProduct(AdminProductForm input)
    {
        if(ModelState.IsValid)try
        {
            var original=services.Catalog.Products(employee).FirstOrDefault(x=>x.MaSanPham==input.Id);
            if(input.Id>0&&original is null)return NotFound();
            byte[]? image=input.RemovePicture?null:original?.AnhBanh;
            if(input.Picture is {Length:>0} file)
            {if(file.Length>5242880)throw new BusinessException("Ảnh tối đa 5 MB.");using var stream=new MemoryStream();await file.CopyToAsync(stream,HttpContext.RequestAborted);image=stream.ToArray();}
            services.Catalog.SaveProduct(employee,new(input.Id,input.CategoryId,input.Name,input.Unit,input.Price,input.Active,input.Description,input.ShelfDays,image,input.Threshold));
            Success("Đã lưu bánh. Trang khách sẽ thấy dữ liệu mới khi tải lại.");return RedirectToAction(nameof(Products));
        }catch(BusinessException ex){Invalid(ex);}
        CategoriesData();return View("Product",input);
    }
    [HttpPost("banh/{id:int}/trang-thai"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult ProductStatus(int id,bool active)
    {var p=services.Catalog.Products(employee).FirstOrDefault(x=>x.MaSanPham==id);if(p is null)return NotFound();try{services.Catalog.SaveProduct(employee,p with{DangKinhDoanh=active});Success("Đã cập nhật trạng thái bánh.");}catch(BusinessException ex){Error(ex);}return RedirectToAction(nameof(Products));}
    [HttpGet("banh/{id:int}/anh")]
    public IActionResult ProductImage(int id)
    {var p=services.Catalog.Products(employee).FirstOrDefault(x=>x.MaSanPham==id);if(p?.AnhBanh is not {Length:>0} bytes)return NotFound();return File(bytes,bytes[0]==137?"image/png":"image/jpeg");}
    private void CategoriesData()=>ViewData["Categories"]=services.Catalog.Categories(employee);

    [HttpGet("danh-muc")]
    public IActionResult Categories()=>View(services.Catalog.Categories(employee));
    [HttpGet("danh-muc/them"),HttpGet("danh-muc/{id:int}/sua"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult Category(int id=0)
    {var c=services.Catalog.Categories(employee).FirstOrDefault(x=>x.MaLoai==id);if(id>0&&c is null)return NotFound();return View(c is null?new AdminCategoryForm():new AdminCategoryForm{Id=c.MaLoai,Name=c.TenLoai,Description=c.MoTa,Active=c.DangSuDung});}
    [HttpPost("danh-muc/luu"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult SaveCategory(AdminCategoryForm input)
    {if(ModelState.IsValid)try{services.Catalog.SaveCategory(employee,new(input.Id,input.Name,input.Description,input.Active));Success("Đã lưu danh mục.");return RedirectToAction(nameof(Categories));}catch(BusinessException ex){Invalid(ex);}return View("Category",input);}
    [HttpPost("danh-muc/{id:int}/xoa"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult DeleteCategory(int id)
    {try{services.Catalog.DeleteCategory(employee,id);Success("Đã xóa danh mục chưa sử dụng.");}catch(BusinessException ex){Error(ex);}return RedirectToAction(nameof(Categories));}

    [HttpGet("kho")]
    public IActionResult Inventory()=>View(new AdminInventory(services.Inventory.Stock(employee),services.Inventory.Lots(employee)));
    [HttpGet("kho/nhap"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult Receive(){ProductsData();return View(new AdminLotForm());}
    [HttpPost("kho/nhap"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult Receive(AdminLotForm input)
    {if(ModelState.IsValid)try{services.Inventory.Receive(employee,new(input.ProductId,input.Source,input.Produced,input.Expiry,input.Quantity));Success("Đã nhập lô bánh.");return RedirectToAction(nameof(Inventory));}catch(BusinessException ex){Invalid(ex);}ProductsData();return View(input);}
    [HttpPost("kho/{id:int}/trang-thai"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult LotStatus(int id,string? status)
    {try{services.Inventory.SetStatus(employee,id,status??"");Success("Đã cập nhật trạng thái lô.");}catch(BusinessException ex){Error(ex);}return RedirectToAction(nameof(Inventory));}
    private void ProductsData()=>ViewData["Products"]=services.Catalog.Products(employee,active:true);

    [HttpGet("khach-hang"),Authorize(Roles=SalesRoles)]
    public IActionResult Customers(string? q=null)=>View(services.Customers.Search(employee,q??""));
    [HttpGet("khach-hang/them"),HttpGet("khach-hang/{id:int}/sua"),Authorize(Roles=SalesRoles)]
    public IActionResult Customer(int id=0)
    {var c=services.Customers.Search(employee,"").FirstOrDefault(x=>x.MaKhachHang==id);if(id>0&&c is null)return NotFound();return View(c is null?new AdminCustomerForm():new AdminCustomerForm{Id=c.MaKhachHang,FullName=c.HoTen,Phone=c.SoDienThoai,Address=c.DiaChi,Note=c.GhiChu});}
    [HttpPost("khach-hang/luu"),Authorize(Roles=SalesRoles)]
    public IActionResult SaveCustomer(AdminCustomerForm input)
    {if(ModelState.IsValid)try{services.Customers.Save(employee,new(input.Id,input.FullName,input.Phone,input.Address,input.Note));Success("Đã lưu hồ sơ khách hàng.");return RedirectToAction(nameof(Customers));}catch(BusinessException ex){Invalid(ex);}return View("Customer",input);}

    [HttpGet("nhan-vien"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult Staff(string? q=null)=>View(services.Staff.Search(employee,q??""));
    [HttpGet("nhan-vien/them"),HttpGet("nhan-vien/{id:int}/sua"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult Employee(int id=0)
    {var s=services.Staff.Search(employee).FirstOrDefault(x=>x.MaNhanVien==id);if(id>0&&s is null)return NotFound();return View(s is null?new AdminStaffForm():new AdminStaffForm{Id=s.MaNhanVien,FullName=s.HoTen,Phone=s.SoDienThoai??"",Role=s.ChucVu,StartDate=s.NgayVaoLam,Active=s.DangLam});}
    [HttpPost("nhan-vien/luu"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult SaveEmployee(AdminStaffForm input)
    {if(ModelState.IsValid)try{var old=services.Staff.Search(employee).FirstOrDefault(x=>x.MaNhanVien==input.Id);if(input.Id>0&&old is null)return NotFound();services.Staff.Save(employee,new(input.Id,input.FullName,input.Phone,input.Role,input.StartDate,input.Active,old?.MaTaiKhoan));Success("Đã lưu nhân viên và cập nhật quyền liên kết.");return RedirectToAction(nameof(Staff));}catch(BusinessException ex){Invalid(ex);}return View("Employee",input);}

    [HttpGet("tai-khoan"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult Accounts()=>View(services.Accounts.List(employee));
    [HttpGet("tai-khoan/them"),HttpGet("tai-khoan/{id:int}/sua"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult Account(int id=0)
    {var a=services.Accounts.List(employee).FirstOrDefault(x=>x.MaTaiKhoan==id);if(id>0&&a is null)return NotFound();StaffData();return View(a is null?new AdminAccountForm():new AdminAccountForm{Id=a.MaTaiKhoan,Username=a.TenDangNhap,FullName=a.HoTen,Role=a.VaiTro,Active=a.TrangThai});}
    [HttpPost("tai-khoan/luu"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult SaveAccount(AdminAccountForm input)
    {
        var staff=services.Staff.Search(employee);
        if(input.Id==0)
        {
            var selected=staff.FirstOrDefault(x=>x.MaNhanVien==input.EmployeeId&&x.DangLam&&x.MaTaiKhoan is null);
            if(selected is null)ModelState.AddModelError("EmployeeId","Chọn nhân viên đang làm, chưa có tài khoản.");
            else{input.FullName=selected.HoTen;input.Role=selected.ChucVu;ModelState.Remove("FullName");ModelState.Remove("Role");}
        }
        else input.EmployeeId=staff.FirstOrDefault(x=>x.MaTaiKhoan==input.Id)?.MaNhanVien;
        if(ModelState.IsValid)try{services.Accounts.Save(employee,input.Id,new(input.Username,input.FullName,input.Role,input.Active,input.Password),input.EmployeeId);Success("Đã lưu tài khoản nhân viên.");return RedirectToAction(nameof(Accounts));}catch(BusinessException ex){Invalid(ex);}
        input.Password="";StaffData();return View("Account",input);
    }
    [HttpPost("tai-khoan/{id:int}/mat-khau"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult ResetPassword(int id,string? password)
    {try{services.Accounts.ResetPassword(employee,id,password??"");Success("Đã đặt mật khẩu tạm. Nhân viên phải đổi khi đăng nhập.");}catch(BusinessException ex){Error(ex);}return RedirectToAction(nameof(Account),new{id});}
    private void StaffData()=>ViewData["Staff"]=services.Staff.Search(employee);

    [HttpGet("hoa-don"),Authorize(Roles=SalesRoles)]
    public IActionResult Invoices()=>View(services.Sales.Invoices(employee));
    [HttpGet("hoa-don/{id:int}"),Authorize(Roles=SalesRoles)]
    public IActionResult Invoice(int id)
    {var invoice=services.Sales.Invoices(employee).FirstOrDefault(x=>x.MaHoaDon==id);if(invoice is null)return NotFound();ViewData["Invoice"]=invoice;return View(services.Sales.Invoice(employee,id));}
    [HttpGet("bao-cao"),Authorize(Roles=Roles.ChuTiem)]
    public IActionResult Reports(DateTime? from=null,DateTime? to=null)
    {
        var start=from??DateTime.Today.AddDays(-29);var end=to??DateTime.Today;ViewData["From"]=start;ViewData["To"]=end;
        try{ViewData["Bestsellers"]=services.Reports.Bestsellers(employee,start,end);return View(services.Reports.Revenue(employee,start,end));}
        catch(BusinessException ex){ModelState.AddModelError("",ex.Message);ViewData["Bestsellers"]=Array.Empty<MocBakery.Models.DTOs.Reports.ProductSales>();return View(Array.Empty<MocBakery.Models.DTOs.Reports.RevenueDay>());}
    }
}
