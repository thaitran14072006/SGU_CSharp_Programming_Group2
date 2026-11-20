using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MocBakery.DAL;
using MocBakery.Models.DTOs.Inventory;

internal static class WarehouseChecks
{
    internal static async Task Run(DbConnectionFactory db,string connection,string root,Func<string,Func<Task>,Task> check)
    {
        await using var app=MocBakery.Web.Program.Build(["--contentRoot",Path.Combine(root,"src/MocBakery.Web"),"--environment","Development"],builder=>
        {builder.Configuration["MOCBAKERY_CONNECTION_STRING"]=connection;builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Logging.ClearProviders();builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();});
        await app.StartAsync();var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        HttpClient Client()=>new(new HttpClientHandler{AllowAutoRedirect=false}){BaseAddress=new(address)};
        using var owner=Client();using var warehouse=Client();using var staff=Client();using var anonymous=Client();
        void Assert(bool ok,string detail="Warehouse assertion failed"){if(!ok)throw new Exception(detail);}
        decimal Value(string sql){using var c=db.Open();using var q=c.CreateCommand();q.CommandText=sql;return Convert.ToDecimal(q.ExecuteScalar());}
        string Token(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        async Task<string> Page(HttpClient c,string route)=>WebUtility.HtmlDecode(await c.GetStringAsync(route));
        async Task<HttpResponseMessage> Post(HttpClient c,string route,string form,Dictionary<string,string> fields)
        {fields["__RequestVerificationToken"]=Token(await c.GetStringAsync(form));return await c.PostAsync(route,new FormUrlEncodedContent(fields));}
        bool Denied(HttpResponseMessage r)=>r.StatusCode==HttpStatusCode.Forbidden||r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("/admin/khong-co-quyen");
        int supplier=0,material=0,receipt=0,lot=0;string version="";var today=DateTime.Today.ToString("yyyy-MM-dd");var expiry=DateTime.Today.AddDays(5).ToString("yyyy-MM-dd");
        string Version(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"Version\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Dictionary<string,string> Draft(int id=0,string? stamp=null,string lotCode="WEB-KHO-CAKE")=>new()
        {
            ["Id"]=id.ToString(),["SupplierId"]=supplier.ToString(),["Date"]=today,["Version"]=stamp??"",["State"]="DaGhiSo",
            ["Lines[0].Item"]="Banh:2",["Lines[0].LotCode"]=lotCode,["Lines[0].Produced"]=today,["Lines[0].Expiry"]=expiry,["Lines[0].Quantity"]="2",["Lines[0].Cost"]="18000",
            ["Lines[1].Item"]="NguyenLieu:"+material,["Lines[1].LotCode"]="WEB-KHO-RAW",["Lines[1].Produced"]=today,["Lines[1].Expiry"]=expiry,["Lines[1].Quantity"]="5.125",["Lines[1].Cost"]="24000",
            ["Lines[1].Unit"]="FORGED",["Total"]="1"
        };
        await check("Warehouse Web authenticates actual owner warehouse and sales roles",async()=>
        {
            foreach(var (client,name,password) in new[]{(owner,"owner","PublicTest123!"),(warehouse,"warehouse.order","Warehouse123!"),(staff,"staff","PublicTest123!")})
            {using var r=await Post(client,"/admin/dang-nhap","/admin/dang-nhap",new(){["Username"]=name,["Password"]=password});Assert(r.StatusCode==HttpStatusCode.Redirect);}
        });
        await check("Warehouse pages challenge anonymous customers",async()=>
        {foreach(var route in new[]{"/admin/phieu-nhap","/admin/nha-cung-cap","/admin/nguyen-lieu","/admin/ton-kho"}){using var r=await anonymous.GetAsync(route);Assert(r.StatusCode==HttpStatusCode.Redirect&&r.Headers.Location!.ToString().Contains("/admin/dang-nhap"));}});
        await check("Warehouse owner pages and forms render with parent navigation",async()=>
        {foreach(var route in new[]{"/admin/phieu-nhap","/admin/phieu-nhap/them","/admin/nha-cung-cap","/admin/nha-cung-cap/them","/admin/nguyen-lieu","/admin/nguyen-lieu/them","/admin/ton-kho","/admin/ton-kho?kind=NguyenLieu","/admin/ton-kho/dieu-chinh?kind=NguyenLieu"}){using var r=await owner.GetAsync(route);Assert(r.StatusCode==HttpStatusCode.OK,route+" returned "+r.StatusCode);}});
        await check("Warehouse writes require antiforgery",async()=>
        {using var r=await owner.PostAsync("/admin/nguyen-lieu/luu",new FormUrlEncodedContent(new Dictionary<string,string>{{"Name","No token"}}));Assert(r.StatusCode==HttpStatusCode.BadRequest);});
        await check("Warehouse supplier form rejects invalid contact without writing",async()=>
        {using var r=await Post(owner,"/admin/nha-cung-cap/luu","/admin/nha-cung-cap/them",new(){["Name"]="Web kho NCC",["Phone"]="123",["Email"]="bad"});Assert(r.StatusCode==HttpStatusCode.OK&&Value("SELECT COUNT(*) FROM dbo.NhaCungCap WHERE TenNCC=N'Web kho NCC'")==0);});
        await check("Warehouse role can create suppliers and materials using shared BLL",async()=>
        {
            using var s=await Post(warehouse,"/admin/nha-cung-cap/luu","/admin/nha-cung-cap/them",new(){["Name"]="Web kho NCC",["Phone"]="02812345678",["Email"]="warehouse@example.com",["Active"]="true"});
            supplier=(int)Value("SELECT MaNCC FROM dbo.NhaCungCap WHERE TenNCC=N'Web kho NCC'");
            using var m=await Post(warehouse,"/admin/nguyen-lieu/luu","/admin/nguyen-lieu/them",new(){["Name"]="Web kho bột",["Unit"]="Kg",["LowStock"]="0.125",["Active"]="true"});
            material=(int)Value("SELECT MaNguyenLieu FROM dbo.NguyenLieu WHERE TenNguyenLieu=N'Web kho bột'");
            Assert(s.StatusCode==HttpStatusCode.Redirect&&m.StatusCode==HttpStatusCode.Redirect&&Value($"SELECT NguongCanhBao FROM dbo.NguyenLieu WHERE MaNguyenLieu={material}")==.125m);
        });
        await check("Sales staff can read warehouse but cannot bypass write permissions",async()=>
        {Assert((await staff.GetAsync("/admin/phieu-nhap")).StatusCode==HttpStatusCode.OK);foreach(var route in new[]{"/admin/phieu-nhap/luu","/admin/nguyen-lieu/luu","/admin/nha-cung-cap/luu","/admin/ton-kho/dieu-chinh"}){using var r=await Post(staff,route,"/admin/phieu-nhap",new(){["Role"]="ChuTiem"});Assert(Denied(r),route);}});
        await check("Warehouse rejects fractional cake quantities and expired receipt dates",async()=>
        {var fields=Draft();fields["Lines[0].Quantity"]="1.5";using var r=await Post(owner,"/admin/phieu-nhap/luu","/admin/phieu-nhap/them",fields);Assert(r.StatusCode==HttpStatusCode.OK&&Value("SELECT COUNT(*) FROM dbo.PhieuNhap")==0);fields=Draft();fields["Lines[0].Expiry"]=DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");using var date=await Post(owner,"/admin/phieu-nhap/luu","/admin/phieu-nhap/them",fields);Assert(date.StatusCode==HttpStatusCode.OK&&Value("SELECT COUNT(*) FROM dbo.PhieuNhap")==0);});
        decimal cakeBefore=Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh WHERE MaSanPham=2");
        await check("Warehouse multiline draft stores server total and real units without changing stock",async()=>
        {
            using var r=await Post(warehouse,"/admin/phieu-nhap/luu","/admin/phieu-nhap/them",Draft());Assert(r.StatusCode==HttpStatusCode.Redirect,"Draft status "+r.StatusCode+" "+Regex.Match(WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync()),"<div[^>]*validation-summary-errors[^>]*>(.*?)</div>",RegexOptions.Singleline).Groups[1].Value);
            receipt=(int)Value("SELECT MAX(MaPhieu) FROM dbo.PhieuNhap");version=Version(await Page(owner,$"/admin/phieu-nhap/{receipt}"));
            Assert(Value($"SELECT SUM(ThanhTien) FROM dbo.ChiTietPhieuNhap WHERE MaPhieu={receipt}")==159000&&Value($"SELECT COUNT(*) FROM dbo.PhieuNhap WHERE MaPhieu={receipt} AND TrangThai=N'Nhap'")==1&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh WHERE MaSanPham=2")==cakeBefore&&Value("SELECT COUNT(*) FROM dbo.LoNguyenLieu")==0&&version.Length>0);
            Assert(Value($"SELECT COUNT(*) FROM dbo.ChiTietPhieuNhap WHERE MaPhieu={receipt} AND DonViTinh=N'FORGED'")==0);
        });
        await check("Warehouse draft rejects missing invalid and stale rowversion",async()=>
        {
            foreach(var stamp in new[]{"","not-base64"}){using var rejected=await Post(owner,"/admin/phieu-nhap/luu",$"/admin/phieu-nhap/{receipt}",Draft(receipt,stamp));Assert(rejected.StatusCode==HttpStatusCode.OK);}
            using var saved=await Post(owner,"/admin/phieu-nhap/luu",$"/admin/phieu-nhap/{receipt}",Draft(receipt,version));Assert(saved.StatusCode==HttpStatusCode.Redirect);
            using var stale=await Post(warehouse,"/admin/phieu-nhap/luu",$"/admin/phieu-nhap/{receipt}",Draft(receipt,version));Assert(stale.StatusCode==HttpStatusCode.OK&&(await stale.Content.ReadAsStringAsync()).Contains("validation-summary-errors"));
        });
        await check("Warehouse posting creates cake material lots and audit atomically",async()=>
        {
            using var r=await Post(warehouse,$"/admin/phieu-nhap/{receipt}/ghi-so",$"/admin/phieu-nhap/{receipt}",new());
            lot=(int)Value("SELECT MaLo FROM dbo.LoNguyenLieu WHERE SoLo=N'WEB-KHO-RAW'");
            Assert(r.StatusCode==HttpStatusCode.Redirect&&Value($"SELECT COUNT(*) FROM dbo.PhieuNhap WHERE MaPhieu={receipt} AND TrangThai=N'DaGhiSo'")==1&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh WHERE MaSanPham=2")==cakeBefore+2&&Value($"SELECT SoLuongCon FROM dbo.LoNguyenLieu WHERE MaLo={lot}")==5.125m&&Value("SELECT COUNT(*) FROM dbo.BienDongKho")==2);
        });
        await check("Warehouse posted receipt is read-only and repeat post does not increase stock",async()=>
        {string html=await Page(owner,$"/admin/phieu-nhap/{receipt}");Assert(!html.Contains(">Lưu nháp</button>")&&html.Contains("disabled"));using var again=await Post(owner,$"/admin/phieu-nhap/{receipt}/ghi-so",$"/admin/phieu-nhap/{receipt}",new());using var edit=await Post(owner,"/admin/phieu-nhap/luu",$"/admin/phieu-nhap/{receipt}",Draft(receipt,version));Assert(edit.StatusCode==HttpStatusCode.OK&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh WHERE MaSanPham=2")==cakeBefore+2&&Value("SELECT COUNT(*) FROM dbo.BienDongKho")==2);});
        await check("Warehouse post failure rolls back all lines and keeps draft",async()=>
        {using var draft=await Post(owner,"/admin/phieu-nhap/luu","/admin/phieu-nhap/them",Draft(lotCode:"WEB-KHO-ROLLBACK"));int id=(int)Value("SELECT MAX(MaPhieu) FROM dbo.PhieuNhap");using var post=await Post(owner,$"/admin/phieu-nhap/{id}/ghi-so",$"/admin/phieu-nhap/{id}",new());Assert(Value("SELECT COUNT(*) FROM dbo.LoBanh WHERE SoLo=N'WEB-KHO-ROLLBACK'")==0&&Value($"SELECT COUNT(*) FROM dbo.PhieuNhap WHERE MaPhieu={id} AND TrangThai=N'Nhap'")==1&&Value("SELECT COUNT(*) FROM dbo.BienDongKho")==2);});
        await check("Warehouse rejects empty stocktake reason without changing stock",async()=>
        {using var r=await Post(owner,"/admin/ton-kho/dieu-chinh",$"/admin/ton-kho/dieu-chinh?kind=NguyenLieu&lotId={lot}",new(){["Kind"]="NguyenLieu",["LotId"]=lot.ToString(),["Operation"]="KiemKe",["Quantity"]="9",["Reason"]=""});Assert(r.StatusCode==HttpStatusCode.OK&&Value($"SELECT SoLuongCon FROM dbo.LoNguyenLieu WHERE MaLo={lot}")==5.125m);});
        await check("Warehouse stocktake derives item from lot and records before after actor and reason",async()=>
        {using var r=await Post(warehouse,"/admin/ton-kho/dieu-chinh",$"/admin/ton-kho/dieu-chinh?kind=NguyenLieu&lotId={lot}",new(){["Kind"]="NguyenLieu",["LotId"]=lot.ToString(),["ItemId"]="999",["Operation"]="KiemKe",["Quantity"]="6.25",["Reason"]="Kiểm kê: thừa nguyên liệu"});Assert(r.StatusCode==HttpStatusCode.Redirect&&Value($"SELECT SoLuongCon FROM dbo.LoNguyenLieu WHERE MaLo={lot}")==6.25m&&Value($"SELECT SoLuongNhap FROM dbo.LoNguyenLieu WHERE MaLo={lot}")==5.125m&&Value($"SELECT COUNT(*) FROM dbo.BienDongKho WHERE MaLoNguyenLieu={lot} AND SoCu=5.125 AND SoMoi=6.25 AND LyDo=N'Kiểm kê: thừa nguyên liệu'")==1);Assert((await Page(owner,$"/admin/ton-kho/NguyenLieu/{lot}/lich-su")).Contains("thừa nguyên liệu"));});
        await check("Warehouse material export cannot exceed stock",async()=>
        {using var r=await Post(owner,"/admin/ton-kho/dieu-chinh","/admin/ton-kho/dieu-chinh?kind=NguyenLieu",new(){["Kind"]="NguyenLieu",["ItemId"]=material.ToString(),["Operation"]="Xuat",["Quantity"]="100",["Reason"]="Sản xuất: vượt tồn"});Assert(r.StatusCode==HttpStatusCode.OK&&Value($"SELECT SoLuongCon FROM dbo.LoNguyenLieu WHERE MaLo={lot}")==6.25m);});
        await check("Warehouse HTTP export allocates material lots in FEFO order",async()=>
        {
            var fields=Draft(lotCode:"WEB-KHO-CAKE-NEAR");fields["Lines[1].LotCode"]="WEB-KHO-RAW-NEAR";fields["Lines[1].Quantity"]="2";fields["Lines[1].Expiry"]=DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
            using var draft=await Post(owner,"/admin/phieu-nhap/luu","/admin/phieu-nhap/them",fields);int id=(int)Value("SELECT MAX(MaPhieu) FROM dbo.PhieuNhap");using var post=await Post(owner,$"/admin/phieu-nhap/{id}/ghi-so",$"/admin/phieu-nhap/{id}",new());
            using var export=await Post(warehouse,"/admin/ton-kho/dieu-chinh","/admin/ton-kho/dieu-chinh?kind=NguyenLieu",new(){["Kind"]="NguyenLieu",["ItemId"]=material.ToString(),["Operation"]="Xuat",["Quantity"]="3",["Reason"]="Sản xuất: kiểm tra Web FEFO"});Assert(export.StatusCode==HttpStatusCode.Redirect&&Value("SELECT SoLuongCon FROM dbo.LoNguyenLieu WHERE SoLo=N'WEB-KHO-RAW-NEAR'")==0&&Value($"SELECT SoLuongCon FROM dbo.LoNguyenLieu WHERE MaLo={lot}")==5.25m);
        });
        await check("Warehouse disposal checkbox controls expired stock handling instead of reason text",async()=>
        {
            using(var c=db.Open()){using var sql=c.CreateCommand();sql.CommandText=$"INSERT dbo.LoNguyenLieu(MaNguyenLieu,SoLo,NgayNhap,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon) VALUES({material},N'WEB-KHO-EXPIRED',GETDATE(),DATEADD(day,-3,CAST(GETDATE() AS DATE)),DATEADD(day,-1,CAST(GETDATE() AS DATE)),5,5)";sql.ExecuteNonQuery();}
            using var normal=await Post(owner,"/admin/ton-kho/dieu-chinh","/admin/ton-kho/dieu-chinh?kind=NguyenLieu",new(){["Kind"]="NguyenLieu",["ItemId"]=material.ToString(),["Operation"]="Xuat",["Quantity"]="1",["Reason"]="Hủy: chỉ là nội dung lý do"});Assert(Value("SELECT SoLuongCon FROM dbo.LoNguyenLieu WHERE SoLo=N'WEB-KHO-EXPIRED'")==5&&Value($"SELECT SoLuongCon FROM dbo.LoNguyenLieu WHERE MaLo={lot}")==4.25m);
            using var disposal=await Post(owner,"/admin/ton-kho/dieu-chinh","/admin/ton-kho/dieu-chinh?kind=NguyenLieu",new(){["Kind"]="NguyenLieu",["ItemId"]=material.ToString(),["Operation"]="Xuat",["Disposal"]="true",["Quantity"]="5",["Reason"]="Lô quá hạn sử dụng"});Assert(disposal.StatusCode==HttpStatusCode.Redirect&&Value("SELECT SoLuongCon FROM dbo.LoNguyenLieu WHERE SoLo=N'WEB-KHO-EXPIRED'")==0&&Value($"SELECT SoLuongCon FROM dbo.LoNguyenLieu WHERE MaLo={lot}")==4.25m);
        });
        await check("Warehouse stock filters reject unknown kind and retain empty-result navigation",async()=>
        {using var invalid=await owner.GetAsync("/admin/ton-kho?kind=Other");using var filter=await owner.GetAsync("/admin/ton-kho?filter=Other");string html=await Page(owner,"/admin/ton-kho?kind=NguyenLieu&q=khong-tim-thay&filter=Expired");Assert(invalid.StatusCode==HttpStatusCode.BadRequest&&filter.StatusCode==HttpStatusCode.BadRequest&&html.Contains("Không có lô phù hợp")&&html.Contains("← Về tổng quan"));});
        await check("Warehouse history and forms reject nonexistent IDs without SQL changes",async()=>
        {foreach(var route in new[]{"/admin/phieu-nhap/999999","/admin/ton-kho/NguyenLieu/999999/lich-su","/admin/nha-cung-cap/999999/sua","/admin/nguyen-lieu/999999/sua"}){using var r=await owner.GetAsync(route);Assert(r.StatusCode==HttpStatusCode.NotFound,route);}});
        await app.StopAsync();
    }
}
