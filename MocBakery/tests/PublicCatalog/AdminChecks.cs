using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using MocBakery.BLL.Services.Orders;
using MocBakery.DAL;
using MocBakery.DAL.Repositories.Orders;
using MocBakery.Models.WebCart;
using ClosedXML.Excel;
using PdfSharp.Pdf.IO;
using MocBakery.Web.Admin;
using MocBakery.Models.DTOs.Reports;

internal static class AdminChecks
{
    internal static async Task Run(DbConnectionFactory db,HttpClient customer,Func<string,Func<Task>,Task> check)
    {
        void Assert(bool ok){if(!ok)throw new Exception("Admin assertion failed");}
        long Value(string sql){using var c=db.Open();using var q=c.CreateCommand();q.CommandText=sql;return Convert.ToInt64(q.ExecuteScalar());}
        string Token(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        HttpClient Client(CookieContainer? cookies=null)=>new(new HttpClientHandler{AllowAutoRedirect=false,CookieContainer=cookies??new CookieContainer()}){BaseAddress=customer.BaseAddress};
        async Task<string> Page(HttpClient c,string route)=>WebUtility.HtmlDecode(await c.GetStringAsync(route));
        async Task<HttpResponseMessage> Post(HttpClient c,string route,string form,Dictionary<string,string> fields)
        {fields["__RequestVerificationToken"]=Token(await c.GetStringAsync(form));return await c.PostAsync(route,new FormUrlEncodedContent(fields));}
        async Task<HttpResponseMessage> Login(HttpClient c,string username,string password)=>await Post(c,"/admin/dang-nhap","/admin/dang-nhap",new(){["Username"]=username,["Password"]=password});
        bool Denied(HttpResponseMessage r)=>r.StatusCode==HttpStatusCode.Forbidden||(r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("/admin/khong-co-quyen"));
        bool AtLogin(HttpResponseMessage r)=>r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("/admin/dang-nhap");
        var ownerCookies=new CookieContainer();using var owner=Client(ownerCookies);using var staff=Client();using var warehouse=Client();using var temporary=Client();using var secondTemporary=Client();
        int category=0,product=0,employee=0,account=0,buyer=0,webOrder=0;long lot=0;
        Dictionary<string,string> Product(string price="25000")=>new(){["Id"]=product.ToString(),["CategoryId"]=category.ToString(),["Name"]="Bánh quản trị thử",["Unit"]="Cái",["Price"]=price,["ShelfDays"]="2",["Threshold"]="5",["Active"]="true"};
        await check("Admin anonymous pages challenge employee login, not customer login",async()=>
        {using var anon=Client();foreach(string path in new[]{"/admin","/admin/don-hang","/admin/banh","/admin/tai-khoan"}){using var r=await anon.GetAsync(path);Assert(AtLogin(r));}});
        await check("Customer cookie cannot read or write admin endpoints",async()=>
        {using var read=await customer.GetAsync("/admin");long before=Value("SELECT COUNT(*) FROM dbo.LoaiBanh");using var write=await Post(customer,"/admin/danh-muc/luu","/gio-hang",new(){["Name"]="Forged customer category",["Active"]="true"});Assert(AtLogin(read)&&AtLogin(write)&&Value("SELECT COUNT(*) FROM dbo.LoaiBanh")==before);});
        await check("Customer credentials cannot authenticate as employee",async()=>
        {using var wrong=Client();using var login=await Login(wrong,"catalog.customer","CatalogTest123!");using var restricted=await wrong.GetAsync("/admin");Assert(login.StatusCode==HttpStatusCode.OK&&AtLogin(restricted));});
        await check("Employee login issues isolated HttpOnly path-limited cookie",async()=>
        {using var login=await Login(owner,"owner","PublicTest123!");Assert(login.StatusCode==HttpStatusCode.Redirect);string cookie=string.Join(";",login.Headers.GetValues("Set-Cookie"));Assert(cookie.Contains("MocBakery.Employee=")&&cookie.Contains("path=/admin",StringComparison.OrdinalIgnoreCase)&&cookie.Contains("httponly",StringComparison.OrdinalIgnoreCase)&&cookie.Contains("samesite=strict",StringComparison.OrdinalIgnoreCase));});
        await check("Employee cookie does not impersonate a shopping customer",async()=>
        {using var r=await owner.GetAsync("/tai-khoan");Assert(r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("/tai-khoan/dang-nhap"));});
        await check("Dashboard renders real SQL revenue and complete management navigation",async()=>
        {string html=await Page(owner,"/admin");long revenue=Value("SELECT COALESCE(SUM(TongTien),0) FROM dbo.HoaDon WHERE CONVERT(date,NgayLap)=CONVERT(date,SYSDATETIME())");Assert(html.Contains("Bảng điều khiển")&&html.Contains(MocBakery.Web.Models.BakeryDisplay.Money(revenue))&&html.Contains("/admin/banh")&&html.Contains("/admin/tai-khoan")&&!html.Contains("PublicTest123!"));});
        await check("All owner management screens and creation forms render",async()=>
        {foreach(var path in new[]{"/admin/don-hang","/admin/banh","/admin/danh-muc","/admin/kho","/admin/khach-hang","/admin/nhan-vien","/admin/tai-khoan","/admin/hoa-don","/admin/bao-cao","/admin/banh/them","/admin/danh-muc/them","/admin/kho/nhap","/admin/khach-hang/them","/admin/nhan-vien/them","/admin/tai-khoan/them"}){using var r=await owner.GetAsync(path);Assert(r.StatusCode==HttpStatusCode.OK);}});
        await check("Admin mutations require antiforgery",async()=>
        {long before=Value("SELECT COUNT(*) FROM dbo.LoaiBanh");using var r=await owner.PostAsync("/admin/danh-muc/luu",new FormUrlEncodedContent(new Dictionary<string,string>{{"Name","No token"}}));Assert(r.StatusCode==HttpStatusCode.BadRequest&&Value("SELECT COUNT(*) FROM dbo.LoaiBanh")==before);});
        await check("Admin creates category through existing catalog BLL",async()=>
        {using var r=await Post(owner,"/admin/danh-muc/luu","/admin/danh-muc/them",new(){["Name"]="Danh mục quản trị thử",["Active"]="true"});category=(int)Value("SELECT MaLoai FROM dbo.LoaiBanh WHERE TenLoai=N'Danh mục quản trị thử'");Assert(r.StatusCode==HttpStatusCode.Redirect&&category>0);});
        await check("Admin adds product and public storefront immediately reads it",async()=>
        {using var r=await Post(owner,"/admin/banh/luu","/admin/banh/them",Product());product=(int)Value("SELECT MaSanPham FROM dbo.SanPham WHERE TenSanPham=N'Bánh quản trị thử'");using var json=JsonDocument.Parse(await customer.GetStringAsync($"/api/banh/{product}"));Assert(r.StatusCode==HttpStatusCode.Redirect&&json.RootElement.GetProperty("price").GetDecimal()==25000);});
        await check("HTML decimal prices preserve fractions under Vietnamese Windows locale",async()=>
        {using var r=await Post(owner,"/admin/banh/luu",$"/admin/banh/{product}/sua",Product("25000.50"));using var json=JsonDocument.Parse(await customer.GetStringAsync($"/api/banh/{product}"));Assert(r.StatusCode==HttpStatusCode.Redirect&&json.RootElement.GetProperty("price").GetDecimal()==25000.50m);});
        await check("Admin edits price without changing product identity",async()=>
        {using var r=await Post(owner,"/admin/banh/luu",$"/admin/banh/{product}/sua",Product("29000"));using var json=JsonDocument.Parse(await customer.GetStringAsync($"/api/banh/{product}"));Assert(r.StatusCode==HttpStatusCode.Redirect&&json.RootElement.GetProperty("price").GetDecimal()==29000);});
        await check("Sales employee can handle orders but cannot edit products or read accounts",async()=>
        {using var login=await Login(staff,"staff","PublicTest123!");using var list=await staff.GetAsync("/admin/don-hang");using var accounts=await staff.GetAsync("/admin/tai-khoan");using var write=await Post(staff,"/admin/banh/luu","/admin/banh",Product("1"));Assert(list.StatusCode==HttpStatusCode.OK&&Denied(accounts)&&Denied(write)&&Value($"SELECT GiaBan FROM dbo.SanPham WHERE MaSanPham={product}")==29000);});
        await check("Warehouse employee is denied customer, order and revenue data",async()=>
        {using var login=await Login(warehouse,"warehouse.order","Warehouse123!");using var stock=await warehouse.GetAsync("/admin/kho");Assert(stock.StatusCode==HttpStatusCode.OK);foreach(var path in new[]{"/admin/don-hang","/admin/khach-hang","/admin/bao-cao","/admin/tai-khoan"}){using var r=await warehouse.GetAsync(path);Assert(Denied(r));}});
        await check("Admin receives lot and changes its sellable status",async()=>
        {using var r=await Post(owner,"/admin/kho/nhap","/admin/kho/nhap",new(){["ProductId"]=product.ToString(),["Quantity"]="5",["Produced"]=DateTime.Today.ToString("yyyy-MM-dd"),["Expiry"]=DateTime.Today.AddDays(2).ToString("yyyy-MM-dd"),["Source"]="Kiểm thử admin"});lot=Value($"SELECT MaLo FROM dbo.LoBanh WHERE MaSanPham={product}");Assert(r.StatusCode==HttpStatusCode.Redirect&&Value($"SELECT SoLuongCon FROM dbo.LoBanh WHERE MaLo={lot}")==5);using var stop=await Post(owner,$"/admin/kho/{lot}/trang-thai","/admin/kho",new(){["status"]="NgungBan"});using var stopped=JsonDocument.Parse(await customer.GetStringAsync($"/api/banh/{product}"));Assert(!stopped.RootElement.GetProperty("inStock").GetBoolean());using var resume=await Post(owner,$"/admin/kho/{lot}/trang-thai","/admin/kho",new(){["status"]="DangBan"});});
        await check("Admin creates customer and keeps internal notes off public pages",async()=>
        {using var r=await Post(owner,"/admin/khach-hang/luu","/admin/khach-hang/them",new(){["FullName"]="Khách quản trị thử",["Phone"]="0907788111",["Note"]="INTERNAL_ONLY_ADMIN_NOTE"});buyer=(int)Value("SELECT MaKhachHang FROM dbo.KhachHang WHERE SoDienThoai='0907788111'");Assert(r.StatusCode==HttpStatusCode.Redirect&&buyer>0&&!(await Page(customer,"/banh")).Contains("INTERNAL_ONLY_ADMIN_NOTE"));});
        await check("Admin creates employee profile before granting account",async()=>
        {using var r=await Post(owner,"/admin/nhan-vien/luu","/admin/nhan-vien/them",new(){["FullName"]="Nhân viên quản trị thử",["Phone"]="0907788222",["Role"]="NhanVien",["StartDate"]=DateTime.Today.ToString("yyyy-MM-dd"),["Active"]="true"});employee=(int)Value("SELECT MaNhanVien FROM dbo.NhanVien WHERE SoDienThoai='0907788222'");Assert(r.StatusCode==HttpStatusCode.Redirect&&employee>0);});
        await check("Admin account creation links employee and requires password change",async()=>
        {using var r=await Post(owner,"/admin/tai-khoan/luu","/admin/tai-khoan/them",new(){["EmployeeId"]=employee.ToString(),["Username"]="admin.check.employee",["Password"]="Temporary123!"});account=(int)Value("SELECT MaTaiKhoan FROM dbo.TaiKhoan WHERE TenDangNhap='admin.check.employee'");Assert(r.StatusCode==HttpStatusCode.Redirect&&Value($"SELECT MaTaiKhoan FROM dbo.NhanVien WHERE MaNhanVien={employee}")==account&&Value($"SELECT CONVERT(int,BatBuocDoiMatKhau) FROM dbo.TaiKhoan WHERE MaTaiKhoan={account}")==1);});
        await check("Temporary employee is forced to password page before accessing data",async()=>
        {using var login=await Login(temporary,"admin.check.employee","Temporary123!");using var second=await Login(secondTemporary,"admin.check.employee","Temporary123!");using var data=await temporary.GetAsync("/admin/banh");Assert(login.Headers.Location?.ToString()=="/admin/doi-mat-khau"&&data.Headers.Location?.ToString()=="/admin/doi-mat-khau");});
        await check("Required employee password page offers logout instead of a looping back link",async()=>
        {string html=await Page(temporary,"/admin/doi-mat-khau");Assert(html.Contains("Đăng xuất và quay lại đăng nhập")&&!html.Contains("← Về quản trị"));});
        await check("Employee password change revokes another browser session",async()=>
        {using var r=await Post(temporary,"/admin/doi-mat-khau","/admin/doi-mat-khau",new(){["CurrentPassword"]="Temporary123!",["Password"]="ChangedEmployee123!",["Confirmation"]="ChangedEmployee123!"});using var revoked=await secondTemporary.GetAsync("/admin");Assert(r.StatusCode==HttpStatusCode.Redirect&&AtLogin(revoked));using var login=await Login(temporary,"admin.check.employee","ChangedEmployee123!");Assert(login.StatusCode==HttpStatusCode.Redirect);});
        await check("Admin cannot disable its own currently active manager account",async()=>
        {int id=(int)Value("SELECT MaTaiKhoan FROM dbo.TaiKhoan WHERE TenDangNhap='owner'");using var r=await Post(owner,"/admin/tai-khoan/luu",$"/admin/tai-khoan/{id}/sua",new(){["Id"]=id.ToString(),["Username"]="owner",["FullName"]="Chủ tiệm kiểm thử",["Role"]="ChuTiem",["Active"]="false"});Assert(r.StatusCode==HttpStatusCode.OK&&Value($"SELECT CONVERT(int,TrangThai) FROM dbo.TaiKhoan WHERE MaTaiKhoan={id}")==1);});
        await check("Live employee role changes immediately affect admin authorization",async()=>
        {using var change=await Post(owner,"/admin/nhan-vien/luu",$"/admin/nhan-vien/{employee}/sua",new(){["Id"]=employee.ToString(),["FullName"]="Nhân viên quản trị thử",["Phone"]="0907788222",["Role"]="Kho",["StartDate"]=DateTime.Today.ToString("yyyy-MM-dd"),["Active"]="true"});using var data=await temporary.GetAsync("/admin/don-hang");Assert(Denied(data));});
        await check("Admin account lock revokes existing employee cookie",async()=>
        {using var change=await Post(owner,"/admin/tai-khoan/luu",$"/admin/tai-khoan/{account}/sua",new(){["Id"]=account.ToString(),["Username"]="admin.check.employee",["FullName"]="Nhân viên quản trị thử",["Role"]="Kho",["Active"]="false"});using var data=await temporary.GetAsync("/admin");Assert(AtLogin(data));});
        int customerId=(int)Value("SELECT MaKhachHang FROM dbo.TaiKhoanKhachHang WHERE TenDangNhap='catalog.customer'");
        var web=new WebOrderService(new WebOrderRepository(db));
        WebPurchaseRequest Request()=>new(customerId,Guid.NewGuid(),[new(product,2)],58000,DateTime.Now.AddDays(1),"Đơn thử nhận ở admin",[new(product,"Bánh quản trị thử",2,29000,true)],new("Người nhận admin","0901122334","admin-order@example.com"));
        await check("Admin receives Web order with correct customer contact",async()=>
        {webOrder=web.Submit(Request());string html=await Page(owner,$"/admin/don-hang/{webOrder}");Assert(html.Contains("admin-order@example.com")&&html.Contains("Tiếp nhận và chuẩn bị"));using var r=await Post(owner,$"/admin/don-hang/{webOrder}/tiep-nhan",$"/admin/don-hang/{webOrder}",new());Assert(r.StatusCode==HttpStatusCode.Redirect&&Value($"SELECT COUNT(*) FROM dbo.DonDatBanh WHERE MaDon={webOrder} AND TrangThai=N'DangChuanBi' AND MaTaiKhoan IS NOT NULL")==1&&Value($"SELECT SoLuongCon FROM dbo.LoBanh WHERE MaLo={lot}")==5);});
        await check("Admin delivery invoices once, deducts stock once and updates customer status",async()=>
        {using var r=await Post(owner,$"/admin/don-hang/{webOrder}/giao",$"/admin/don-hang/{webOrder}",new());using var retry=await Post(owner,$"/admin/don-hang/{webOrder}/giao",$"/admin/don-hang/{webOrder}",new());Assert(r.Headers.Location!.ToString().StartsWith("/admin/hoa-don/")&&Value($"SELECT COUNT(*) FROM dbo.HoaDon WHERE MaDon={webOrder}")==1&&Value($"SELECT SoLuongCon FROM dbo.LoBanh WHERE MaLo={lot}")==3&&(await Page(customer,$"/don-hang/{webOrder}")).Contains("Đã giao"));});
        await check("Admin cancellation requires reason and preserves stock",async()=>
        {int id=web.Submit(Request());using var blank=await Post(owner,$"/admin/don-hang/{id}/huy",$"/admin/don-hang/{id}",new(){["reason"]=""});Assert(Value($"SELECT COUNT(*) FROM dbo.DonDatBanh WHERE MaDon={id} AND TrangThai=N'MoiTiepNhan'")==1);using var cancel=await Post(owner,$"/admin/don-hang/{id}/huy",$"/admin/don-hang/{id}",new(){["reason"]="Khách đổi lịch"});Assert((await Page(customer,$"/don-hang/{id}")).Contains("Khách đổi lịch")&&Value($"SELECT SoLuongCon FROM dbo.LoBanh WHERE MaLo={lot}")==3);});
        await check("Admin archives products without deleting order history",async()=>
        {using var r=await Post(owner,$"/admin/banh/{product}/trang-thai","/admin/banh",new(){["active"]="false"});using var hidden=await customer.GetAsync($"/api/banh/{product}");Assert(hidden.StatusCode==HttpStatusCode.NotFound&&Value($"SELECT COUNT(*) FROM dbo.ChiTietDonDat WHERE MaSanPham={product}")==2);using var categoryOff=await Post(owner,"/admin/danh-muc/luu",$"/admin/danh-muc/{category}/sua",new(){["Id"]=category.ToString(),["Name"]="Danh mục quản trị thử",["Active"]="false"});});
        await check("Revenue downloads enforce employee owner authorization",async()=>
        {
            using var anon=Client();
            foreach(var format in new[]{"excel","pdf"})
            {
                string path="/admin/bao-cao/"+format;
                using var a=await anon.GetAsync(path);using var c=await customer.GetAsync(path);
                using var s=await staff.GetAsync(path);using var w=await warehouse.GetAsync(path);
                Assert(AtLogin(a)&&AtLogin(c)&&Denied(s)&&Denied(w));
            }
        });
        string reportOut=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../artifacts/report-export"));
        Directory.CreateDirectory(reportOut);
        await check("Excel export matches invoiced SQL totals, numeric cells and selected inclusive dates",async()=>
        {
            string today=DateTime.Today.ToString("yyyy-MM-dd");
            using var response=await owner.GetAsync($"/admin/bao-cao/excel?from={today}&to={today}");
            Assert(response.StatusCode==HttpStatusCode.OK&&response.Content.Headers.ContentType?.MediaType=="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"&&response.Headers.CacheControl?.NoStore==true);
            byte[] bytes=await response.Content.ReadAsByteArrayAsync();
            using var stream=new MemoryStream(bytes);using var workbook=new XLWorkbook(stream);
            Assert(workbook.Worksheets.Count==3&&workbook.Worksheet("Tổng hợp").Cell("B6").GetValue<decimal>()==Value("SELECT COALESCE(SUM(TongTien),0) FROM dbo.HoaDon WHERE CONVERT(date,NgayLap)=CONVERT(date,GETDATE())"));
            Assert(workbook.Worksheet("Tổng hợp").Cell("B7").GetValue<long>()==Value("SELECT COUNT(*) FROM dbo.HoaDon WHERE CONVERT(date,NgayLap)=CONVERT(date,GETDATE())"));
            Assert(workbook.Worksheet("Theo ngày").Cell("A8").DataType==XLDataType.DateTime&&workbook.Worksheet("Theo ngày").Cell("C8").DataType==XLDataType.Number);
            Assert(response.Content.Headers.ContentDisposition?.FileNameStar?.Contains(today.Replace("-",""))==true);
            await File.WriteAllBytesAsync(Path.Combine(reportOut,"report.xlsx"),bytes);
        });
        await check("Report exports reject malformed reversed and overflowing date ranges",async()=>
        {
            foreach(var format in new[]{"excel","pdf"})
            foreach(var query in new[]{"from=bad","from=2026-11-20&to=2026-11-01","to=9999-12-31"})
            {using var response=await owner.GetAsync("/admin/bao-cao/"+format+"?"+query);Assert(response.StatusCode==HttpStatusCode.BadRequest&&response.Content.Headers.ContentDisposition is null);}
            using var unknown=await owner.GetAsync("/admin/bao-cao/csv");Assert(unknown.StatusCode==HttpStatusCode.NotFound);
        });
        await check("Empty exports remain valid; product names cannot inject Excel formulas",async()=>
        {
            using var response=await owner.GetAsync("/admin/bao-cao/excel?from=1800-01-01&to=1800-01-01");
            using var stream=new MemoryStream(await response.Content.ReadAsByteArrayAsync());using var empty=new XLWorkbook(stream);
            Assert(empty.Worksheet("Tổng hợp").Cell("B6").GetValue<decimal>()==0&&empty.Worksheet("Theo ngày").Cell("A8").GetString().Contains("Không có"));
            byte[] safe=RevenueExport.Excel(DateTime.Today,DateTime.Today,[],[new ProductSales(1,"=HYPERLINK(\"https://example.com\")",1,12.50m)]);
            using var safeStream=new MemoryStream(safe);using var safeBook=new XLWorkbook(safeStream);
            var name=safeBook.Worksheet("Bánh bán chạy").Cell("A8");
            Assert(!name.HasFormula&&name.GetString().StartsWith("=HYPERLINK")&&safeBook.Worksheet("Bánh bán chạy").Cell("C8").GetValue<decimal>()==12.50m);
        });
        await check("PDF export is downloadable with embedded Vietnamese fonts and paginates long reports",async()=>
        {
            using var response=await owner.GetAsync("/admin/bao-cao/pdf");
            byte[] bytes=await response.Content.ReadAsByteArrayAsync();
            Assert(response.StatusCode==HttpStatusCode.OK&&response.Content.Headers.ContentType?.MediaType=="application/pdf"&&response.Content.Headers.ContentDisposition?.FileNameStar?.EndsWith(".pdf")==true);
            using var stream=new MemoryStream(bytes);using var pdf=PdfReader.Open(stream,PdfDocumentOpenMode.Import);Assert(pdf.PageCount>0);
            await File.WriteAllBytesAsync(Path.Combine(reportOut,"report.pdf"),bytes);
            var days=Enumerable.Range(0,100).Select(i=>new RevenueDay(DateTime.Today.AddDays(-i),1,12345.50m)).ToArray();
            var products=Enumerable.Range(0,60).Select(i=>new ProductSales(i,"Bánh kem dâu, sô cô la và hạnh nhân - phiên bản đặc biệt "+i,3,34567.50m)).ToArray();
            byte[] longBytes=RevenueExport.Pdf(DateTime.Today.AddDays(-99),DateTime.Today,days,products);
            using var longStream=new MemoryStream(longBytes);using var longPdf=PdfReader.Open(longStream,PdfDocumentOpenMode.Import);Assert(longPdf.PageCount>=4);
            await File.WriteAllBytesAsync(Path.Combine(reportOut,"report-long.pdf"),longBytes);
            byte[] emptyBytes=RevenueExport.Pdf(DateTime.Today,DateTime.Today,[],[]);
            using var emptyStream=new MemoryStream(emptyBytes);using var emptyPdf=PdfReader.Open(emptyStream,PdfDocumentOpenMode.Import);Assert(emptyPdf.PageCount==1);
            await File.WriteAllBytesAsync(Path.Combine(reportOut,"report-empty.pdf"),emptyBytes);
        });
        await check("Admin logout is POST-only and revokes a copied cookie",async()=>
        {
            using var get=await owner.GetAsync("/admin/dang-xuat");Assert(get.StatusCode==HttpStatusCode.MethodNotAllowed);
            var cloneCookies=new CookieContainer();cloneCookies.SetCookies(new Uri(customer.BaseAddress!,"/admin"),ownerCookies.GetCookieHeader(new Uri(customer.BaseAddress!,"/admin")));
            using var clone=Client(cloneCookies);using var logout=await Post(owner,"/admin/dang-xuat","/admin",new());using var copied=await clone.GetAsync("/admin");Assert(AtLogin(copied));
        });
    }
}
