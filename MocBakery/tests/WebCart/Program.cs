using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MocBakery.BLL.Services.Catalog;
using MocBakery.BLL.Services.Sales;
using MocBakery.Models;
using MocBakery.Models.PublicCatalog;
using MocBakery.Models.WebCart;
using MocBakery.Web.Security;

var catalog = new Catalog();
var cart = new CartService(new PublicCatalogService(catalog));
int passed = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
void Reject(Action action, string name) { try { action(); } catch (BusinessException) { Check(true, name); return; } throw new Exception(name); }
Check(cart.Quote([]).CanSubmit == false, "empty cart blocked");
var items = cart.Change([], 1, 2, true);
items = cart.Change(items, 1, 1, true);
Check(items.Single().Quantity == 3 && cart.Quote(items).Total == 45000, "add merges quantity and recalculates total");
Reject(() => cart.Change(items, 1, -1, false), "negative quantity rejected");
Reject(() => cart.Change(items, 1, 1001, false), "quantity limit enforced");
Reject(() => cart.Change(items, 1, 10, true), "insufficient stock rejected");
Check(cart.Change(items, 1, 0, false).Count == 0, "remove item");
catalog.Active = false;
Check(!cart.Quote(items).CanSubmit, "inactive product invalidates existing cart");
catalog.Active = true;
catalog.Price = 20000;
Check(cart.Quote(items).Total == 60000, "quote uses current server price");
catalog.Price = 15000;
var orders = new Orders();
var app = MocBakery.Web.Program.Build(["--environment", "Development"], builder =>
{
    builder.WebHost.UseUrls("http://127.0.0.1:0");
    builder.Logging.ClearProviders();
    builder.Services.AddSingleton<IPublicCatalogRepository>(catalog);
    builder.Services.AddSingleton<IWebOrderSubmission>(orders);
    builder.Services.AddAuthentication(options => { options.DefaultAuthenticateScheme = "test"; options.DefaultChallengeScheme = "test"; })
        .AddScheme<AuthenticationSchemeOptions, TestAuth>("test", _ => { });
});
await app.StartAsync();
try
{
    string url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var client = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer(), AllowAutoRedirect = false }) { BaseAddress = new Uri(url) };
    async Task<string> Page(string path) => await client.GetStringAsync(path);
    string Token(string html, string name) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"" + name + "\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    async Task<HttpResponseMessage> Post(string path, string html, params (string, string)[] fields)
    {
        var values = fields.ToDictionary(x => x.Item1, x => x.Item2);
        if(path=="/gio-hang/xac-nhan"&&!values.ContainsKey("pickup"))values["pickup"]=DateTime.Now.AddDays(1).ToString("s");
        values["__RequestVerificationToken"] = Token(html, "__RequestVerificationToken");
        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }
    string detail = await Page("/banh/1");
    Check(detail.Contains("Thêm vào giỏ"), "detail renders add form");
    var csrf = await client.PostAsync("/gio-hang/them", new FormUrlEncodedContent(new Dictionary<string,string>{{"productId","1"},{"quantity","2"}}));
    Check(csrf.StatusCode == HttpStatusCode.BadRequest, "mutation requires antiforgery");
    await Post("/gio-hang/them", detail, ("productId","1"), ("quantity","2"), ("price","1"));
    string page = await Page("/gio-hang");
    Check(page.Contains("30.000"), "HTTP cart ignores forged price");
    var anonymous = await client.GetAsync("/gio-hang/xac-nhan");
    Check(anonymous.StatusCode == HttpStatusCode.Unauthorized, "review requires customer authentication");
    client.DefaultRequestHeaders.Add("X-Test-Customer", "yes");
    page = await Page("/gio-hang/xac-nhan");
    string key = Token(page, "requestKey");
    Check(Guid.TryParse(key, out _), "review issues submission key");
    catalog.Price = 16000;
    await Post("/gio-hang/xac-nhan", page, ("requestKey", key), ("customerId","999"));
    Check(orders.Count == 0, "price changed after review blocks submission");
    page = await Page("/gio-hang/xac-nhan"); key = Token(page, "requestKey");
    orders.Fail = true;
    await Post("/gio-hang/xac-nhan", page, ("requestKey", key));
    Check((await Page("/gio-hang")).Contains("32.000"), "submission failure preserves cart");
    orders.Fail = false;
    orders.FailDb = true;
    await Post("/gio-hang/xac-nhan", page, ("requestKey", key));
    string failure=await Page("/gio-hang/xac-nhan");
    Check((await Page("/gio-hang")).Contains("32.000")&&!failure.Contains("private database"),"database failure preserves cart without exposing details");
    orders.FailDb = false;
    await Post("/gio-hang/xac-nhan", page, ("requestKey", key), ("customerId","999"), ("total","1"));
    Check(orders.Count == 1 && orders.Last?.CustomerId == 7 && orders.Last.ConfirmedTotal == 32000, "submission uses authenticated customer and server total");
    await Post("/gio-hang/xac-nhan", page, ("requestKey", key));
    Check(orders.Count == 1, "sequential repeat cannot create second order");
    Check((await Page("/gio-hang")).Contains("Giỏ hàng đang trống"), "successful submission clears cart");
    Console.WriteLine($"{passed} checks passed. SQL and live Nam integration not exercised.");
}
finally { await app.StopAsync(); await app.DisposeAsync(); }

sealed class Catalog : IPublicCatalogRepository
{
    public decimal Price = 15000;
    public bool Active = true;
    public CatalogProductSnapshot? Find(int id) => Active && id == 1 ? new(1,1,"Bánh thử","Bánh mì","cái",Price,"Bánh mẫu",false,10) : null;
    public IReadOnlyList<PublicCategory> Categories() => [new(1,"Bánh mì",1)];
    public CatalogSnapshotPage Browse(PublicCatalogQuery query) => new(Find(1) is {} p ? [p] : [], Active ? 1 : 0);
    public byte[]? Image(int id) => null;
}
sealed class Orders : IWebOrderSubmission
{
    private readonly Dictionary<Guid,int> ids = [];
    public int Count => ids.Count;
    public WebPurchaseRequest? Last;
    public bool Fail;
    public bool FailDb;
    public int Submit(WebPurchaseRequest request)
    {
        if(Fail) throw new BusinessException("Thử lỗi tạo đơn.");
        if(FailDb) throw new InvalidOperationException("private database connection");
        Last = request;
        if(!ids.TryGetValue(request.RequestKey,out int id)) ids[request.RequestKey] = id = ids.Count + 1;
        return id;
    }
}
sealed class TestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
        Request.Headers.ContainsKey("X-Test-Customer") ? AuthenticateResult.Success(new AuthenticationTicket(
            CustomerCookie.Principal(new(1,7,Guid.NewGuid()), "khach-thu"), Scheme.Name)) : AuthenticateResult.NoResult());
}
