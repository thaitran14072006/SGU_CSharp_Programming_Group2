using System.Data.Common;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.Catalog;
using MocBakery.BLL.Services.Inventory;
using MocBakery.DAL;
using MocBakery.DAL.Repositories.Accounts;
using MocBakery.DAL.Repositories.Catalog;
using MocBakery.DAL.Repositories.Inventory;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.PublicCatalog;
using MocBakery.Web.Models;
using WebHost = MocBakery.Web.Program;

// New isolated LocalDB database on every run. No shared/business database is used.
string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../.."));
string database="MocBakery_PublicTest_"+Guid.NewGuid().ToString("N");
var master=new DbConnectionFactory(@"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=15");
string connection=$@"Server=(localdb)\MSSQLLocalDB;Database={database};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=15";
var db=new DbConnectionFactory(connection);
int passed=0,failed=0;
void Assert(bool value){if(!value)throw new Exception("Assertion failed");}
async Task Check(string name,Func<Task> test){try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}}
void Reject(Action action){try{action();}catch(BusinessException){return;}throw new Exception("Expected BusinessException");}
void Sql(string sql){using var c=db.Open();using var q=c.CreateCommand();q.CommandText=sql;q.ExecuteNonQuery();}
try
{
    using(var c=master.Open())
    {
        using(var q=c.CreateCommand()){q.CommandText=$"CREATE DATABASE [{database}] COLLATE Vietnamese_CI_AS";q.ExecuteNonQuery();}
        string schema=File.ReadAllText(Path.Combine(root,"database/schema/01_CreateDatabase.sql")).Replace("QuanLyTiemBanh",database,StringComparison.Ordinal);
        foreach(string batch in Regex.Split(schema,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
        {if(string.IsNullOrWhiteSpace(batch))continue;using var q=c.CreateCommand();q.CommandText=batch;q.ExecuteNonQuery();}
    }
    var accounts=new TaiKhoanRepository(db);var auth=new AuthService(accounts);
    accounts.Create(new("owner","Chủ tiệm kiểm thử",Roles.ChuTiem,true,PasswordHasher.Hash("PublicTest123!")));
    accounts.Create(new("staff","Nhân viên kiểm thử",Roles.NhanVien,true,PasswordHasher.Hash("PublicTest123!")));
    var owner=auth.Login("owner","PublicTest123!");var staff=auth.Login("staff","PublicTest123!");
    var categories=new LoaiBanhRepository(db);var products=new SanPhamRepository(db);var lots=new LoBanhRepository(db);
    var desktop=new CatalogService(categories,products,auth);
    DateOnly today;using(var c=db.Open()){using var q=c.CreateCommand();q.CommandText="SELECT CONVERT(date,SYSDATETIME())";today=DateOnly.FromDateTime((DateTime)q.ExecuteScalar()!);}
    var inventory=new InventoryService(lots,products,auth,()=>today);
    int sweet=desktop.SaveCategory(owner,new(0,"Bánh ngọt",null));
    int bread=desktop.SaveCategory(owner,new(0,"Bánh mì",null));
    int empty=desktop.SaveCategory(owner,new(0,"Bánh theo mùa",null));
    int hidden=desktop.SaveCategory(owner,new(0,"Danh mục nội bộ",null,false));
    int cake=desktop.SaveProduct(owner,new(0,sweet,"Tiramisu","Cái",45000,true,"Vị cà phê và kem phô mai."));
    int croissant=desktop.SaveProduct(owner,new(0,bread,"Croissant","Cái",28000,true,"Bánh sừng bò."));
    int cookie=desktop.SaveProduct(owner,new(0,sweet,"Cookie","Hộp",18000,true,"Bánh quy."));
    int unavailable=desktop.SaveProduct(owner,new(0,sweet,"Muffin","Cái",35000,true,null));
    int stoppedProduct=desktop.SaveProduct(owner,new(0,sweet,"Bánh ngừng bán","Cái",10000,false,null));
    // Historical product may remain assigned to a stopped category on desktop.
    int hiddenProduct=desktop.SaveProduct(owner,new(0,sweet,"Bánh nội bộ","Cái",12000,true,null));
    Sql($"UPDATE dbo.SanPham SET MaLoai={hidden} WHERE MaSanPham={hiddenProduct}");
    inventory.Receive(owner,new(cake,null,today,today,3));
    inventory.Receive(owner,new(cake,null,today,today.AddDays(2),4));
    inventory.Receive(owner,new(cake,null,today.AddDays(-2),today.AddDays(-1),50));
    int stoppedLot=inventory.Receive(owner,new(cake,null,today,today.AddDays(1),60));inventory.SetStatus(owner,stoppedLot,LotStatus.Stopped);
    Sql($"INSERT dbo.LoBanh(MaSanPham,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon) VALUES({cake},DATEADD(day,1,CONVERT(date,SYSDATETIME())),DATEADD(day,2,CONVERT(date,SYSDATETIME())),70,70)");
    inventory.Receive(owner,new(croissant,null,today,today.AddDays(1),20));
    var service=new PublicCatalogService(new PublicCatalogRepository(db));
    await using var app=WebHost.Build(["--contentRoot",Path.Combine(root,"src/MocBakery.Web"),"--environment","Development"],builder=>
    {
        builder.Configuration["MOCBAKERY_CONNECTION_STRING"]=connection;
        builder.WebHost.UseUrls(args.Contains("--serve")?"http://127.0.0.1:5088":"http://127.0.0.1:0");
        builder.Logging.ClearProviders();builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
    });
    await app.StartAsync();
    string address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var client=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){BaseAddress=new Uri(address)};
    async Task<string> Page(string path)=>WebUtility.HtmlDecode(await client.GetStringAsync(path));
    async Task<JsonDocument> Json(string path)=>JsonDocument.Parse(await client.GetStringAsync(path));
    async Task Status(string path,HttpStatusCode status){using var r=await client.GetAsync(path);Assert(r.StatusCode==status);}
    await Check("Only active products in active categories are public",()=>{Assert(service.Browse(new()).Total==4&&service.Find(stoppedProduct)==null&&service.Find(hiddenProduct)==null);return Task.CompletedTask;});
    await Check("Empty categories remain discoverable; inactive categories hidden",()=>{var cats=service.Categories();Assert(cats.Count==3&&cats.Single(x=>x.Id==empty).ProductCount==0&&cats.Single(x=>x.Id==sweet).ProductCount==3);return Task.CompletedTask;});
    await Check("Sellable SQL stock excludes expired stopped future lots; includes expiry day",()=>{Assert(service.CheckSelection(cake,7)!.CanFulfill&&!service.CheckSelection(cake,8)!.CanFulfill);return Task.CompletedTask;});
    await Check("Desktop and Web agree on eligible stock",()=>{Assert(inventory.Stock(owner).Single(p=>p.MaSanPham==cake).TonCoTheBan==7&&service.Find(cake)!.InStock);return Task.CompletedTask;});
    await Check("Out of stock product remains visible without available flag",()=>{Assert(!service.Find(unavailable)!.InStock&&service.Browse(new(Availability:"out-of-stock")).Total==2);return Task.CompletedTask;});
    await Check("Trimmed name and product code searches",()=>{Assert(service.Browse(new("  Tiramisu  ")).Items.Single().Id==cake&&service.Browse(new($"B{cake:000}")).Items.Single().Id==cake);return Task.CompletedTask;});
    await Check("LIKE wildcards are literal and parameterized",()=>
    {
        var original=products.List().Single(p=>p.MaSanPham==cookie);
        desktop.SaveProduct(owner,original with{TenSanPham="Cookie 100% _ [Mộc] ~"});
        try{foreach(var key in new[]{"%","_","[","~"})Assert(service.Browse(new(key)).Items.Single().Id==cookie);Assert(service.Browse(new("' OR 1=1 --")).Total==0);}
        finally{desktop.SaveProduct(owner,original);}
        return Task.CompletedTask;
    });
    await Check("Category, stock filters and stable price sorting combine",()=>{var result=service.Browse(new(CategoryId:sweet,Availability:"in-stock",Sort:"price-desc"));Assert(result.Items.Single().Id==cake);Assert(service.Browse(new(Sort:"price-asc")).Items.First().Id==cookie);return Task.CompletedTask;});
    await Check("Pagination keeps totals and supports beyond last page",()=>{var r=service.Browse(new(Page:2,PageSize:2));Assert(r.Total==4&&r.PageCount==2&&r.Items.Count==2&&service.Browse(new(Page:9)).Items.Count==0);return Task.CompletedTask;});
    await Check("Invalid filters and excessive page sizes fail validation",()=>{foreach(var q in new[]{new PublicCatalogQuery(new string('x',101)),new(CategoryId:0),new(Availability:"hack"),new(Sort:"DROP TABLE"),new(Page:0),new(PageSize:0),new(PageSize:25)})Reject(()=>service.Browse(q));return Task.CompletedTask;});
    await Check("Selection validation rejects invalid quantities and missing IDs",()=>{Reject(()=>service.CheckSelection(cake,0));Reject(()=>service.CheckSelection(cake,1001));Assert(service.CheckSelection(-1,1)==null&&service.CheckSelection(stoppedProduct,1)==null);return Task.CompletedTask;});
    await Check("Vietnamese price formatting preserves fractional currency",()=>{Assert(BakeryDisplay.Money(25000.50m)=="25.000,50 ₫"&&BakeryDisplay.Money(28000)=="28.000 ₫");return Task.CompletedTask;});
    await Check("Home is public and shares account navigation",async()=>{string html=await Page("/");Assert(html.Contains("Một chút ngọt")&&html.Contains("/tai-khoan/dang-nhap")&&html.Contains("/danh-muc")&&html.Contains("/css/site.css"));});
    await Check("Home category navigation includes active and empty categories only",async()=>{string html=await Page("/");Assert(html.Contains("aria-label=\"Chọn danh mục bánh\"")&&html.Contains($"href=\"/danh-muc/{sweet}\"")&&html.Contains($"href=\"/danh-muc/{empty}\"")&&!html.Contains($"href=\"/danh-muc/{hidden}\""));});
    await Check("Public categories and empty category render",async()=>{Assert((await Page("/danh-muc")).Contains("Bánh theo mùa"));Assert((await Page($"/danh-muc/{empty}")).Contains("Chưa tìm thấy bánh phù hợp"));});
    await Check("Search GET renders results and preserves filters",async()=>{string html=await Page("/banh?q=Tiramisu&sort=price-desc&availability=in-stock");Assert(html.Contains("Tiramisu")&&!html.Contains("href=\"/banh/"+croissant+"\"")&&html.Contains("value=\"Tiramisu\""));});
    await Check("Zero-match search is a helpful 200 result",async()=>Assert((await Page("/banh?q=khong-tim-thay")).Contains("Chưa tìm thấy bánh phù hợp")));
    await Check("XSS input is encoded by Razor",async()=>{string raw=await client.GetStringAsync("/banh?q="+Uri.EscapeDataString("<script>alert(1)</script>"));Assert(!raw.Contains("<script>alert(1)</script>"));});
    await Check("Invalid numeric query returns 400 instead of silently ignoring",async()=>{await Status("/banh?page=abc",HttpStatusCode.BadRequest);await Status("/api/banh?categoryId=abc",HttpStatusCode.BadRequest);});
    await Check("Invalid filter values return 400 on HTML and API",async()=>{await Status("/banh?sort=nope",HttpStatusCode.BadRequest);await Status("/api/banh?pageSize=999",HttpStatusCode.BadRequest);});
    await Check("Missing invalid discontinued and hidden category IDs return 404",async()=>{foreach(int id in new[]{-1,0,999999,stoppedProduct,hiddenProduct}){await Status($"/banh/{id}",HttpStatusCode.NotFound);await Status($"/api/banh/{id}",HttpStatusCode.NotFound);await Status($"/banh/{id}/anh",HttpStatusCode.NotFound);}await Status($"/danh-muc/{hidden}",HttpStatusCode.NotFound);});
    await Check("Out of stock detail explains availability",async()=>Assert((await Page($"/banh/{unavailable}")).Contains("Tạm hết bánh")));
    await Check("API exposes approved fields only without internal stock or credentials",async()=>{using var json=await Json($"/api/banh/{cake}");var fields=json.RootElement.EnumerateObject().Select(p=>p.Name).Order().ToArray();Assert(fields.SequenceEqual(new[]{"id","categoryId","code","name","category","unit","price","description","hasImage","inStock","imageUrl"}.Order()));});
    await Check("Public API supports listing and category contract",async()=>{using var list=await Json("/api/banh?pageSize=2");using var cats=await Json("/api/danh-muc");Assert(list.RootElement.GetProperty("total").GetInt32()==4&&list.RootElement.GetProperty("items").GetArrayLength()==2&&cats.RootElement.GetArrayLength()==3);});
    await Check("Catalog routes cannot write via POST",async()=>{using var r=await client.PostAsync($"/api/banh/{cake}",new StringContent("{\"price\":1}"));Assert(r.StatusCode==HttpStatusCode.MethodNotAllowed);});
    await Check("Account login still renders with shared layout",async()=>Assert((await Page("/tai-khoan/dang-nhap")).Contains("__RequestVerificationToken")));
    await Check("Catalog response disables caching and keeps security headers",async()=>{using var r=await client.GetAsync($"/api/banh/{cake}");Assert(r.Headers.CacheControl?.NoStore==true&&r.Headers.Contains("X-Content-Type-Options")&&r.Headers.Contains("Content-Security-Policy"));});
    await Check("Desktop owner price edit is visible after HTTP reload and selection recheck",async()=>{var p=products.List().Single(p=>p.MaSanPham==cake);desktop.SaveProduct(owner,p with{GiaBan=49000});using var json=await Json($"/api/banh/{cake}");Assert(json.RootElement.GetProperty("price").GetDecimal()==49000&&service.CheckSelection(cake,1)!.CurrentUnitPrice==49000&&(await Page($"/banh/{cake}")).Contains("49.000"));});
    byte[] png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jM1sAAAAASUVORK5CYII=");
    await Check("Desktop image edit is visible on uncached image endpoint",async()=>{desktop.SaveProduct(owner,products.List().Single(p=>p.MaSanPham==cake) with{AnhBanh=png});using var r=await client.GetAsync($"/banh/{cake}/anh");Assert(r.Content.Headers.ContentType?.MediaType=="image/png"&&(await r.Content.ReadAsByteArrayAsync()).SequenceEqual(png)&&r.Headers.CacheControl?.NoStore==true&&(await Page($"/banh/{cake}")).Contains($"src=\"/banh/{cake}/anh\""));});
    await Check("Removing desktop image restores the matching supplied product photo",async()=>{desktop.SaveProduct(owner,products.List().Single(p=>p.MaSanPham==cake) with{AnhBanh=null});await Status($"/banh/{cake}/anh",HttpStatusCode.NotFound);Assert((await Page($"/banh/{cake}")).Contains("src=\"/images/bakery/tiramisu.jpg\""));});
    await Check("Corrupt or executable image payload is never served",async()=>{Sql($"UPDATE dbo.SanPham SET AnhBanh=0x3C7363726970743E WHERE MaSanPham={cake}");await Status($"/banh/{cake}/anh",HttpStatusCode.NotFound);desktop.SaveProduct(owner,products.List().Single(p=>p.MaSanPham==cake) with{AnhBanh=null});});
    await Check("Desktop deactivation removes product from HTML API image and cart contract",async()=>{desktop.SaveProduct(owner,products.List().Single(p=>p.MaSanPham==cake) with{DangKinhDoanh=false});await Status($"/banh/{cake}",HttpStatusCode.NotFound);await Status($"/api/banh/{cake}",HttpStatusCode.NotFound);Assert(service.CheckSelection(cake,1)==null&&service.Browse(new("Tiramisu")).Total==0);desktop.SaveProduct(owner,products.List().Single(p=>p.MaSanPham==cake) with{DangKinhDoanh=true});});
    await Check("Category stop hides products but preserves desktop history",async()=>{desktop.SaveCategory(owner,categories.List().Single(c=>c.MaLoai==bread) with{DangSuDung=false});await Status($"/banh/{croissant}",HttpStatusCode.NotFound);Assert(desktop.Products(owner).Any(p=>p.MaSanPham==croissant));desktop.SaveCategory(owner,categories.List().Single(c=>c.MaLoai==bread) with{DangSuDung=true});});
    await Check("Staff cannot bypass desktop product management permission",()=>{Reject(()=>desktop.SaveProduct(staff,products.List().Single(p=>p.MaSanPham==cake) with{GiaBan=1}));Assert(service.Find(cake)!.Price==49000);return Task.CompletedTask;});
    await Check("Desktop stock stop is reflected on Web reload",async()=>{foreach(var l in lots.GetSellableLots(cake,today))inventory.SetStatus(owner,l.MaLo,LotStatus.Stopped);using var json=await Json($"/api/banh/{cake}");Assert(!json.RootElement.GetProperty("inStock").GetBoolean()&&!service.CheckSelection(cake,1)!.CanFulfill);});
    await Check("HTML pagination retains search category availability and sort",async()=>
    {
        var ids=new List<int>();
        try
        {
            for(int i=1;i<=13;i++)ids.Add(desktop.SaveProduct(owner,new(0,sweet,$"Bánh phân trang {i:00}","Cái",10000+i,true,null)));
            string html=await client.GetStringAsync($"/banh?q="+Uri.EscapeDataString("Bánh phân trang")+$"&categoryId={sweet}&availability=out-of-stock&sort=price-desc");
            var links=Regex.Matches(html,"href=\"([^\"]+)\"").Select(m=>WebUtility.HtmlDecode(m.Groups[1].Value));
            string next=links.Single(s=>s.Contains("page=2"));
            Assert(next.Contains("sort=price-desc")&&next.Contains("availability=out-of-stock")&&(next.Contains($"categoryId={sweet}")||next.StartsWith($"/danh-muc/{sweet}?",StringComparison.Ordinal))&&next.Contains("q="));
            string page=await Page(next);Assert(Regex.Matches(page,"<article class=\"product\">").Count==1&&page.Contains("Trang 2 / 2"));
        }
        finally{foreach(int id in ids)desktop.SaveProduct(owner,products.List().Single(p=>p.MaSanPham==id) with{DangKinhDoanh=false});}
    });
    await Check("Database outage returns helpful 503 without internal details",async()=>
    {
        await using var offline=WebHost.Build(["--contentRoot",Path.Combine(root,"src/MocBakery.Web"),"--environment","Development"],builder=>
        {
            builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Logging.ClearProviders();
            builder.Services.AddSingleton<IPublicCatalogRepository>(new OfflineCatalog());
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
        await offline.StartAsync();
        using var http=new HttpClient{BaseAddress=new Uri(offline.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single())};
        foreach(string route in new[]{"/","/banh","/api/banh","/api/danh-muc"})
        {using var response=await http.GetAsync(route);string body=await response.Content.ReadAsStringAsync();Assert(response.StatusCode==HttpStatusCode.ServiceUnavailable&&body.Contains("Vui lòng thử lại")&&!body.Contains("SECRET")&&!body.Contains("SqlException"));}
        await offline.StopAsync();
    });
    // Apply Thái's existing account migration only to this isolated test database.
    Sql(File.ReadAllText(Path.Combine(root,"database/schema/04_CustomerWebAccounts.sql")));
    using var customer=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false,CookieContainer=new CookieContainer()}){BaseAddress=new Uri(address)};
    async Task<HttpResponseMessage> CustomerPost(string route,Dictionary<string,string> data)
    {
        string form=await customer.GetStringAsync(route);
        data["__RequestVerificationToken"]=WebUtility.HtmlDecode(Regex.Match(form,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        return await customer.PostAsync(route,new FormUrlEncodedContent(data));
    }
    await Check("SQL customer login works across shared storefront navigation",async()=>
    {
        using var register=await CustomerPost("/tai-khoan/dang-ky",new(){["Username"]="catalog.customer",["Password"]="CatalogTest123!",["Confirmation"]="CatalogTest123!",["FullName"]="Khách thử Catalog",["Phone"]="0900001122"});
        Assert(register.StatusCode==HttpStatusCode.Redirect);
        using var login=await CustomerPost("/tai-khoan/dang-nhap",new(){["Username"]="catalog.customer",["Password"]="CatalogTest123!"});
        Assert(login.StatusCode==HttpStatusCode.Redirect);
        string html=WebUtility.HtmlDecode(await customer.GetStringAsync("/banh"));
        Assert(html.Contains("Tài khoản")&&html.Contains("Đăng xuất")&&!html.Contains(">Đăng nhập</a>")&&html.Contains("Croissant"));
    });
    await Check("SQL profile edit keeps customer session and storefront prices",async()=>
    {
        using var save=await CustomerPost("/tai-khoan/ho-so",new(){["FullName"]="Khách đã cập nhật",["Phone"]="0900001122",["Address"]="Địa chỉ kiểm thử"});
        Assert(save.StatusCode==HttpStatusCode.Redirect);
        string profile=WebUtility.HtmlDecode(await customer.GetStringAsync("/tai-khoan"));
        string detail=WebUtility.HtmlDecode(await customer.GetStringAsync($"/banh/{cake}"));
        Assert(profile.Contains("Khách đã cập nhật")&&profile.Contains("Thông tin hồ sơ đã được cập nhật")&&detail.Contains("49.000")&&detail.Contains("Đăng xuất"));
    });
    await WebOrderChecks.Run(db,customer,croissant,auth,owner,root,Check);
    Console.WriteLine($"RESULT {passed} passed, {failed} failed");
    if(args.Contains("--serve")&&failed==0)
    {
        Console.WriteLine("PREVIEW "+address+" (isolated fixture; press Enter to stop and clean up)");
        await Task.WhenAny(app.WaitForShutdownAsync(),Task.Run(()=>Console.ReadLine()));
    }
    await app.StopAsync();Environment.ExitCode=failed==0?0:1;
}

finally
{
    // Only the exact random database created by this process may be removed.
    if(Regex.IsMatch(database,@"^MocBakery_PublicTest_[a-f0-9]{32}$"))
    {using var c=master.Open();using var q=c.CreateCommand();q.CommandText=$"IF DB_ID(N'{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END";q.ExecuteNonQuery();}
}

sealed class OfflineCatalog : IPublicCatalogRepository
{
    public IReadOnlyList<PublicCategory> Categories()=>throw new InvalidOperationException("SECRET SQL connection");
    public CatalogSnapshotPage Browse(PublicCatalogQuery query)=>throw new InvalidOperationException("SECRET SQL connection");
    public CatalogProductSnapshot? Find(int id)=>throw new InvalidOperationException("SECRET SQL connection");
    public byte[]? Image(int id)=>throw new InvalidOperationException("SECRET SQL connection");
}
