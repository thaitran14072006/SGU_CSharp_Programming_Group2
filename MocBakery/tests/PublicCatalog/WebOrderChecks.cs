using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.CustomerAccounts;
using MocBakery.BLL.Services.Orders;
using MocBakery.DAL;
using MocBakery.DAL.Repositories.CustomerAccounts;
using MocBakery.DAL.Repositories.Orders;
using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;
using MocBakery.Models.WebCart;

internal static class WebOrderChecks
{
    internal static async Task Run(DbConnectionFactory db,HttpClient customer,int product,AuthService auth,UserSession owner,
        string root,Func<string,Func<Task>,Task> check)
    {
        void Assert(bool ok){if(!ok)throw new Exception("Order assertion failed");}
        void Reject(Action action){try{action();}catch(BusinessException){return;}throw new Exception("Expected order validation rejection");}
        void Sql(string sql){using var c=db.Open();using var q=c.CreateCommand();q.CommandText=sql;q.ExecuteNonQuery();}
        long Value(string sql){using var c=db.Open();using var q=c.CreateCommand();q.CommandText=sql;return Convert.ToInt64(q.ExecuteScalar());}
        string Token(string html,string name)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\""+name+"\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        async Task<string> Page(string path)=>WebUtility.HtmlDecode(await customer.GetStringAsync(path));
        async Task<HttpResponseMessage> Post(string route,string tokenPage,Dictionary<string,string> fields)
        {
            fields["__RequestVerificationToken"]=Token(await customer.GetStringAsync(tokenPage),"__RequestVerificationToken");
            return await customer.PostAsync(route,new FormUrlEncodedContent(fields));
        }
        var customerAccounts=new CustomerAccountService(new CustomerAccountRepository(db));
        int customerId=customerAccounts.Login("catalog.customer","CatalogTest123!").CustomerId;
        var desktop=new OrderService(new OrderRepository(db),auth);
        int counter=desktop.Create(owner,customerId,DateTime.Now.AddDays(1),0,"Đơn quầy trước nâng cấp",[new(product,"Croissant",1,28000)]);
        string migration=File.ReadAllText(Path.Combine(root,"database/schema/05_WebOrders.sql"));
        Sql(migration);
        var service=new WebOrderService(new WebOrderRepository(db));
        long WebCount()=>Value("SELECT COUNT(*) FROM dbo.DonDatBanh WHERE WebRequestKey IS NOT NULL");
        long Stock()=>Value($"SELECT SUM(CONVERT(bigint,SoLuongCon)) FROM dbo.LoBanh WHERE MaSanPham={product}");
        WebPurchaseRequest Request(Guid? key=null)=>new(customerId,key??Guid.NewGuid(),[new(product,2)],56000,DateTime.Now.AddDays(1),"Đóng hộp riêng",[new(product,"Croissant",2,28000,true)]);
        int received=0,parallel=0;
        Guid requestKey=Guid.Empty;
        await check("Order migration reruns without changing existing desktop history",()=>
        {Sql(migration);var old=desktop.List(owner).Single(x=>x.Id==counter);Assert(old.AccountId==owner.MaTaiKhoan&&old.Note=="Đơn quầy trước nâng cấp"&&old.Total==28000);return Task.CompletedTask;});
        await check("Web order BLL rejects invalid identities quantities duplicates and missing pickup",()=>
        {
            var valid=Request();
            foreach(var invalid in new[]{valid with{CustomerId=0},valid with{RequestKey=Guid.Empty},valid with{Items=[]},valid with{Items=[new(product,0)]},valid with{Items=[new(product,1001)]},valid with{Items=[new(product,1),new(product,1)]},valid with{ConfirmedTotal=0},valid with{Pickup=null},valid with{ConfirmedLines=null},valid with{Note=new string('x',501)}})Reject(()=>service.Submit(invalid));
            Assert(WebCount()==0);return Task.CompletedTask;
        });
        await check("Checkout requires antiforgery and a future pickup without losing cart",async()=>
        {
            using var add=await Post("/gio-hang/them",$"/banh/{product}",new(){["productId"]=product.ToString(),["quantity"]="2"});
            string review=await Page("/gio-hang/xac-nhan");requestKey=Guid.Parse(Token(review,"requestKey"));
            using var noToken=await customer.PostAsync("/gio-hang/xac-nhan",new FormUrlEncodedContent(new Dictionary<string,string>{{"requestKey",requestKey.ToString()}}));
            Assert(noToken.StatusCode==HttpStatusCode.BadRequest);
            using var missing=await Post("/gio-hang/xac-nhan","/gio-hang/xac-nhan",new(){["requestKey"]=requestKey.ToString()});
            using var past=await Post("/gio-hang/xac-nhan","/gio-hang/xac-nhan",new(){["requestKey"]=requestKey.ToString(),["pickup"]=DateTime.Now.AddDays(-1).ToString("s")});
            Assert(WebCount()==0&&(await Page("/gio-hang")).Contains("56.000"));
        });
        await check("HTTP checkout writes SQL order using cookie customer and server prices",async()=>
        {
            using var result=await Post("/gio-hang/xac-nhan","/gio-hang/xac-nhan",new(){["requestKey"]=requestKey.ToString(),["pickup"]=DateTime.Now.AddDays(1).ToString("s"),["note"]="Hộp riêng ' <b>không cay</b>",["customerId"]="999",["total"]="1"});
            Assert(result.StatusCode==HttpStatusCode.Redirect&&result.Headers.Location!.ToString().StartsWith("/don-hang/"));
            received=int.Parse(result.Headers.Location!.ToString().Split('/').Last());
            var order=service.Find(customerId,received)!;
            Assert(order.Order.Total==56000&&order.Lines.Single().Price==28000&&order.Lines.Single().Quantity==2&&WebCount()==1);
        });
        await check("Web reception has no forged employee no invoice and no stock deduction",()=>
        {var order=desktop.List(owner).Single(x=>x.Id==received);Assert(order.AccountId is null&&order.Status=="MoiTiepNhan"&&Value("SELECT COUNT(*) FROM dbo.HoaDon")==0&&Stock()==20);return Task.CompletedTask;});
        await check("Receipt is readable by owner and safely encodes order notes",async()=>
        {string raw=await customer.GetStringAsync($"/don-hang/{received}");Assert(WebUtility.HtmlDecode(raw).Contains("Mới tiếp nhận")&&!raw.Contains("<b>không cay</b>")&&(await Page("/don-hang")).Contains($"/don-hang/{received}")&&(await Page("/gio-hang")).Contains("Giỏ hàng đang trống"));});
        await check("HTTP repeated checkout returns same receipt and creates no duplicate",async()=>
        {using var retry=await Post("/gio-hang/xac-nhan","/gio-hang",new(){["requestKey"]=requestKey.ToString()});Assert(retry.Headers.Location?.ToString()==$"/don-hang/{received}"&&WebCount()==1);});
        await check("Parallel SQL retries share one persistent order id",async()=>
        {var request=Request();var ids=await Task.WhenAll(Enumerable.Range(0,8).Select(_=>Task.Run(()=>service.Submit(request))));parallel=ids[0];Assert(ids.Distinct().Count()==1&&WebCount()==2&&Stock()==20);});
        await check("Repository rechecks price inside transaction before writing",()=>
        {long before=WebCount();Reject(()=>service.Submit(Request() with{ConfirmedTotal=1}));Assert(WebCount()==before);return Task.CompletedTask;});
        await check("Repository rechecks live stock before writing",()=>
        {long before=WebCount();Reject(()=>service.Submit(Request() with{Items=[new(product,100)],ConfirmedTotal=2800000,ConfirmedLines=[new(product,"Croissant",100,28000,true)]}));Assert(WebCount()==before&&Stock()==20);return Task.CompletedTask;});
        await check("Per-line prices are rechecked even when two changes leave total unchanged",()=>
        {
            int cookie=(int)Value("SELECT MaSanPham FROM dbo.SanPham WHERE TenSanPham LIKE N'Cookie%'");
            long lot=Value($"INSERT dbo.LoBanh(MaSanPham,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon) OUTPUT INSERTED.MaLo VALUES({cookie},CONVERT(date,SYSDATETIME()),DATEADD(day,1,CONVERT(date,SYSDATETIME())),1,1)");
            var request=Request() with{Items=[new(product,1),new(cookie,1)],ConfirmedTotal=46000,ConfirmedLines=[new(product,"Croissant",1,28000,true),new(cookie,"Cookie",1,18000,true)]};
            long before=WebCount();Sql($"UPDATE dbo.SanPham SET GiaBan=29000 WHERE MaSanPham={product}; UPDATE dbo.SanPham SET GiaBan=17000 WHERE MaSanPham={cookie}");
            try{Reject(()=>service.Submit(request));Assert(WebCount()==before);}
            finally{Sql($"UPDATE dbo.SanPham SET GiaBan=28000 WHERE MaSanPham={product}; UPDATE dbo.SanPham SET GiaBan=18000 WHERE MaSanPham={cookie}; DELETE dbo.LoBanh WHERE MaLo={lot}");}
            return Task.CompletedTask;
        });
        await check("Repository rejects stopped products and stopped categories",()=>
        {
            long before=WebCount();Sql($"UPDATE dbo.SanPham SET DangKinhDoanh=0 WHERE MaSanPham={product}");
            try{Reject(()=>service.Submit(Request()));}finally{Sql($"UPDATE dbo.SanPham SET DangKinhDoanh=1 WHERE MaSanPham={product}");}
            Sql($"UPDATE dbo.LoaiBanh SET DangSuDung=0 WHERE MaLoai=(SELECT MaLoai FROM dbo.SanPham WHERE MaSanPham={product})");
            try{Reject(()=>service.Submit(Request()));}finally{Sql($"UPDATE dbo.LoaiBanh SET DangSuDung=1 WHERE MaLoai=(SELECT MaLoai FROM dbo.SanPham WHERE MaSanPham={product})");}
            Assert(WebCount()==before);return Task.CompletedTask;
        });
        await check("Repository rejects past pickup and disabled customer",()=>
        {
            Reject(()=>service.Submit(Request() with{Pickup=DateTime.Now.AddDays(-1)}));
            Sql($"UPDATE dbo.TaiKhoanKhachHang SET TrangThai=0 WHERE MaKhachHang={customerId}");
            try{Reject(()=>service.Submit(Request()));}finally{Sql($"UPDATE dbo.TaiKhoanKhachHang SET TrangThai=1 WHERE MaKhachHang={customerId}");}
            Assert(WebCount()==2);return Task.CompletedTask;
        });
        await check("Failed detail insert rolls back the header and all order lines",()=>
        {
            long before=Value("SELECT COUNT(*) FROM dbo.ChiTietDonDat");
            Sql("CREATE TRIGGER dbo.TestRejectWebLine ON dbo.ChiTietDonDat AFTER INSERT AS THROW 51001, 'Fixture rejection', 1;");
            bool failed=false;try{service.Submit(Request());}catch(System.Data.Common.DbException){failed=true;}finally{Sql("DROP TRIGGER dbo.TestRejectWebLine");}
            Assert(failed&&WebCount()==2&&Value("SELECT COUNT(*) FROM dbo.ChiTietDonDat")==before&&Stock()==20);return Task.CompletedTask;
        });
        await check("Another customer cannot read the receipt by changing the URL",async()=>
        {
            customerAccounts.Register(new("other.customer","OtherTest123!","OtherTest123!","Khách khác","0900003344",null));
            int otherId=customerAccounts.Login("other.customer","OtherTest123!").CustomerId;
            Assert(service.Find(otherId,received)==null&&service.List(otherId).Count==0);
            using var other=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false,CookieContainer=new CookieContainer()}){BaseAddress=customer.BaseAddress};
            using var anonymous=await other.GetAsync($"/don-hang/{received}");Assert(anonymous.StatusCode==HttpStatusCode.Redirect);
            string login=await other.GetStringAsync("/tai-khoan/dang-nhap");
            using var signed=await other.PostAsync("/tai-khoan/dang-nhap",new FormUrlEncodedContent(new Dictionary<string,string>{{"Username","other.customer"},{"Password","OtherTest123!"},{"__RequestVerificationToken",Token(login,"__RequestVerificationToken")}}));
            using var forbidden=await other.GetAsync($"/don-hang/{received}");Assert(forbidden.StatusCode==HttpStatusCode.NotFound);
        });
        await check("Warehouse employee cannot accept or deliver customer orders",()=>
        {
            var accounts=new MocBakery.DAL.Repositories.Accounts.TaiKhoanRepository(db);
            accounts.Create(new("warehouse.order","Thủ kho",Roles.Kho,true,PasswordHasher.Hash("Warehouse123!")));
            var warehouse=auth.Login("warehouse.order","Warehouse123!");Reject(()=>desktop.Prepare(warehouse,received));Reject(()=>desktop.Deliver(warehouse,received));return Task.CompletedTask;
        });
        await check("Desktop acceptance records the real employee and updates customer status",async()=>
        {desktop.Prepare(owner,received);var row=desktop.List(owner).Single(x=>x.Id==received);Assert(row.AccountId==owner.MaTaiKhoan&&row.Status=="DangChuanBi"&&Stock()==20&&Value("SELECT COUNT(*) FROM dbo.HoaDon")==0&&(await Page($"/don-hang/{received}")).Contains("Đang chuẩn bị"));});
        await check("Desktop delivery creates one invoice and deducts FEFO stock exactly once",async()=>
        {int invoice=desktop.Deliver(owner,received);Reject(()=>desktop.Deliver(owner,received));Assert(invoice>0&&Stock()==18&&Value($"SELECT COUNT(*) FROM dbo.HoaDon WHERE MaDon={received}")==1&&(await Page($"/don-hang/{received}")).Contains("Đã giao"));});
        await check("Desktop cancellation is visible to customer without selling or deducting stock",async()=>
        {desktop.Cancel(owner,parallel,"Khách đổi lịch nhận");Reject(()=>desktop.Prepare(owner,parallel));Assert(Stock()==18&&Value($"SELECT COUNT(*) FROM dbo.HoaDon WHERE MaDon={parallel}")==0&&(await Page($"/don-hang/{parallel}")).Contains("Khách đổi lịch nhận"));});
    }
}
