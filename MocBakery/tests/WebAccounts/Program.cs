using System.Data.Common;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MocBakery.BLL.Services.CustomerAccounts;
using MocBakery.Models;
using MocBakery.Models.CustomerAccounts;
using WebHost = MocBakery.Web.Program;

if (args.Contains("--sql"))
{
    Environment.ExitCode = await SqlAccountChecks.Run();
    return;
}

int passed = 0, failed = 0;
void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
async Task Check(string title, Func<Task> test)
{
    try { await test(); passed++; Console.WriteLine("PASS " + title); }
    catch (Exception ex) { failed++; Console.WriteLine("FAIL " + title + ": " + ex.Message); }
}
var repository = new WebFixtureRepository();
string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/MocBakery.Web"));
await using var app = WebHost.Build(["--contentRoot", root, "--environment", "Development"], builder =>
{
    builder.WebHost.UseUrls("http://127.0.0.1:0");
    builder.Logging.ClearProviders();
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
    builder.Services.AddSingleton<ICustomerAccountRepository>(repository);
});
await app.StartAsync();
string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
using var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() };
using var client = new HttpClient(handler) { BaseAddress = new Uri(address) };
async Task<string> Page(string path) => await client.GetStringAsync(path);
static string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html,
    "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
static Dictionary<string, string> Registration(string username = "web.customer", string phone = "0901234567") => new()
{
    ["Username"] = username, ["Password"] = "WebPassword123!", ["Confirmation"] = "WebPassword123!",
    ["FullName"] = "Khách Web", ["Phone"] = phone, ["Address"] = "Địa chỉ Web"
};
async Task<HttpResponseMessage> Post(string path, Dictionary<string, string> values, string? tokenPage = null)
{
    values["__RequestVerificationToken"] = Token(await Page(tokenPage ?? path));
    return await client.PostAsync(path, new FormUrlEncodedContent(values));
}
static Dictionary<string, string> Login(string password = "WebPassword123!") => new() { ["Username"] = "web.customer", ["Password"] = password };
const string register = "/tai-khoan/dang-ky", login = "/tai-khoan/dang-nhap", profile = "/tai-khoan";
try
{
    await Check("Registration page renders a real antiforgery form", async () =>
    {
        string html = await Page(register);
        Assert(Token(html).Length > 20 && html.Contains("lang=\"vi\"") && html.Contains("name=\"FullName\""));
    });
    await Check("Responsive account stylesheet is served locally", async () =>
        Assert((await Page("/css/accounts.css")).Contains("@media(max-width:580px)")));
    await Check("Anonymous customer profile redirects to login", async () =>
    {
        using var response = await client.GetAsync(profile);
        Assert(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.ToString().Contains(login));
    });
    await Check("Registration without antiforgery token writes nothing", async () =>
    {
        using var response = await client.PostAsync(register, new FormUrlEncodedContent(Registration()));
        Assert(response.StatusCode == HttpStatusCode.BadRequest && repository.Accounts.Count == 0);
    });
    await Check("Invalid confirmation is rejected server side without writing", async () =>
    {
        var data = Registration(); data["Confirmation"] = "WrongPassword!";
        using var response = await Post(register, data);
        string html = await response.Content.ReadAsStringAsync();
        Assert(response.StatusCode == HttpStatusCode.OK && repository.Accounts.Count == 0 && html.Contains("validation-summary-errors"));
        Assert(!html.Contains("value=\"WebPassword123!\"") && !html.Contains("value=\"WrongPassword!\""));
    });
    await Check("Valid registration redirects to login and creates shared profile", async () =>
    {
        using var response = await Post(register, Registration());
        Assert(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.ToString().Contains(login));
        Assert(repository.Accounts.Count == 1 && repository.Profiles[1].Phone == "0901234567");
    });
    await Check("Duplicate login is rejected and preserves non-secret inputs", async () =>
    {
        using var response = await Post(register, Registration("WEB.CUSTOMER", "0900000002"));
        string html = await response.Content.ReadAsStringAsync();
        Assert(repository.Accounts.Count == 1 && WebUtility.HtmlDecode(html).Contains("Tên đăng nhập đã tồn tại") && html.Contains("0900000002"));
    });
    await Check("Existing customer phone cannot be claimed by public signup", async () =>
    {
        using var response = await Post(register, Registration("different.customer"));
        Assert(repository.Accounts.Count == 1 && WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()).Contains("Số điện thoại đã có hồ sơ"));
    });
    await Check("Wrong password does not issue a login cookie or echo password", async () =>
    {
        using var response = await Post(login, Login("IncorrectPassword!"));
        Assert(response.StatusCode == HttpStatusCode.OK
            && (!response.Headers.TryGetValues("Set-Cookie", out var cookies)
                || !cookies.Any(cookie => cookie.StartsWith("MocBakery.Customer=", StringComparison.Ordinal)))
            && !(await response.Content.ReadAsStringAsync()).Contains("IncorrectPassword!"));
    });
    await Check("Valid login issues HttpOnly SameSite cookie and rejects external return URL", async () =>
    {
        var data = Login(); data["returnUrl"] = "https://example.invalid/"; data["CustomerId"] = "999"; data["Role"] = "ChuTiem";
        using var response = await Post(login, data);
        string cookie = response.Headers.GetValues("Set-Cookie").Single(v => v.StartsWith("MocBakery.Customer="));
        Assert(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.ToString() == profile);
        Assert(cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) && cookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));
    });
    await Check("Customer login resumes checkout and refuses an admin return destination", async () =>
    {
        var checkout = Login(); checkout["ReturnUrl"] = "/gio-hang/xac-nhan";
        using var resumed = await Post(login, checkout);
        Assert(resumed.Headers.Location?.ToString() == "/gio-hang/xac-nhan");
        var admin = Login(); admin["ReturnUrl"] = "/admin";
        using var rejected = await Post(login, admin);
        Assert(rejected.Headers.Location?.ToString() == profile);
    });
    await Check("Login and registration discard unsafe or looping return links", async () =>
    {
        foreach (string target in new[] { "https://example.invalid", "//example.invalid", "/admin", "/tai-khoan/dang-nhap" })
        foreach (string route in new[] { login, register })
        {
            string html = await Page(route + "?returnUrl=" + Uri.EscapeDataString(target));
            Assert(!Regex.IsMatch(html, "name=\"ReturnUrl\"[^>]*value=\"[^\"]+\""));
        }
    });
    await Check("Authenticated page reads only the signed-in customer profile", async () =>
    {
        string html = WebUtility.HtmlDecode(await Page(profile));
        Assert(html.Contains("Khách Web") && html.Contains("0901234567") && !html.Contains("SecurityStamp") && !html.Contains("MatKhauHash"));
    });
    await Check("Profile HTML escapes customer input", async () =>
    {
        repository.Profiles[1] = repository.Profiles[1] with { FullName = "<script>alert(1)</script>" };
        string html = await Page(profile);
        Assert(!html.Contains("<script>alert(1)</script>") && html.Contains("&lt;script&gt;"));
    });
    await Check("Account response prevents caching and framing", async () =>
    {
        using var response = await client.GetAsync(profile);
        Assert(response.Headers.CacheControl!.NoStore && response.Headers.GetValues("X-Frame-Options").Single() == "DENY");
    });
    const string edit = profile + "/ho-so";
    await Check("Anonymous browsers cannot read or post profile edits", async () =>
    {
        using var anonymous = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = client.BaseAddress };
        using var get = await anonymous.GetAsync(edit);
        using var post = await anonymous.PostAsync(edit, new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert(get.StatusCode == HttpStatusCode.Redirect && post.StatusCode == HttpStatusCode.Redirect);
    });
    await Check("Edit form reads only the cookie owner's profile", async () =>
    {
        repository.Profiles[99] = new("Other customer", "0909999999", "Private other address");
        string html = await Page(edit + "?CustomerId=99");
        Assert(html.Contains("0901234567") && !html.Contains("0909999999") && Token(html).Length > 20
            && !html.Contains("name=\"CustomerId\"") && !html.Contains("name=\"Role\"") && !html.Contains("name=\"GhiChu\""));
    });
    await Check("Profile POST without antiforgery token leaves data intact", async () =>
    {
        var before = repository.Profiles[1];
        using var response = await client.PostAsync(edit, new FormUrlEncodedContent(new Dictionary<string, string>
            { ["FullName"] = "Unwanted", ["Phone"] = "0900000005" }));
        Assert(response.StatusCode == HttpStatusCode.BadRequest && repository.Profiles[1] == before);
    });
    await Check("Profile validation rejects blank name and malformed phone", async () =>
    {
        var before = repository.Profiles[1];
        using var response = await Post(edit, new() { ["FullName"] = " ", ["Phone"] = "123" });
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert(response.StatusCode == HttpStatusCode.OK && repository.Profiles[1] == before
            && html.Contains("Nhập họ tên") && html.Contains("10 chữ số"));
    });
    await Check("Profile server validation enforces name and address limits", async () =>
    {
        var before = repository.Profiles[1];
        using var response = await Post(edit, new() { ["FullName"] = new('n', 101), ["Phone"] = "0901234567", ["Address"] = new('a', 256) });
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert(response.StatusCode == HttpStatusCode.OK && repository.Profiles[1] == before && html.Contains("100 ký tự") && html.Contains("255 ký tự"));
    });
    await Check("Duplicate profile phone shows field error and retains draft", async () =>
    {
        var before = repository.Profiles[1];
        using var response = await Post(edit, new() { ["FullName"] = "Draft customer", ["Phone"] = "0909999999", ["Address"] = "Draft address" });
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert(response.StatusCode == HttpStatusCode.OK && repository.Profiles[1] == before && html.Contains("Số điện thoại đã có hồ sơ")
            && html.Contains("Draft customer") && html.Contains("Draft address"));
    });
    await Check("Profile update ignores forged ids and employee fields", async () =>
    {
        var other = repository.Profiles[99];
        var account = repository.Accounts[1];
        using var response = await Post(edit, new()
        {
            ["FullName"] = "  Nguyễn   Bình ", ["Phone"] = "0900000004", ["Address"] = "  Hồ Chí Minh  ",
            ["CustomerId"] = "99", ["AccountId"] = "99", ["Role"] = "ChuTiem", ["GhiChu"] = "Alter internal note"
        });
        Assert(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.ToString() == profile
            && repository.Profiles[1] == new CustomerProfile("Nguyễn Bình", "0900000004", "Hồ Chí Minh")
            && repository.Profiles[99] == other && repository.Accounts[1] == account);
    });
    await Check("Profile success redirects to GET and refresh does not resubmit", async () =>
    {
        int writes = repository.ProfileWrites;
        string first = WebUtility.HtmlDecode(await Page(profile));
        string refresh = WebUtility.HtmlDecode(await Page(profile));
        Assert(first.Contains("Thông tin hồ sơ đã được cập nhật") && !refresh.Contains("Thông tin hồ sơ đã được cập nhật")
            && refresh.Contains("Nguyễn Bình") && repository.ProfileWrites == writes);
    });
    await Check("Profile accepts a 100-character name and 255-character address", async () =>
    {
        using var response = await Post(edit, new() { ["FullName"] = new('n', 100), ["Phone"] = "0900000004", ["Address"] = new('a', 255) });
        Assert(response.StatusCode == HttpStatusCode.Redirect && repository.Profiles[1].FullName.Length == 100 && repository.Profiles[1].Address!.Length == 255);
    });
    await Check("Profile optional address can be cleared", async () =>
    {
        using var response = await Post(edit, new() { ["FullName"] = "Nguyễn Bình", ["Phone"] = "0900000004", ["Address"] = " " });
        Assert(response.StatusCode == HttpStatusCode.Redirect && repository.Profiles[1].Address is null);
    });
    await Check("Profile database failure preserves saved data and entered draft", async () =>
    {
        var before = repository.Profiles[1];
        repository.Unavailable = true;
        try
        {
            using var response = await Post(edit, new() { ["FullName"] = "Draft after outage", ["Phone"] = "0900000004", ["Address"] = "Draft address" });
            string html = await response.Content.ReadAsStringAsync();
            Assert(response.StatusCode == HttpStatusCode.ServiceUnavailable && repository.Profiles[1] == before
                && html.Contains("Draft after outage") && !html.Contains("FixtureSecret"));
        }
        finally { repository.Unavailable = false; }
    });
    await Check("Session revoked during profile save cannot write and requires login", async () =>
    {
        var before = repository.Profiles[1];
        repository.RevokeOnNextProfileWrite = true;
        using var response = await Post(edit, new() { ["FullName"] = "Stale edit", ["Phone"] = "0900000004" });
        using var after = await client.GetAsync(profile);
        Assert(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.ToString() == login
            && after.StatusCode == HttpStatusCode.Redirect && repository.Profiles[1] == before);
        using var signedInAgain = await Post(login, Login());
        Assert(signedInAgain.StatusCode == HttpStatusCode.Redirect);
    });
    await Check("Tampered cookie cannot authenticate", async () =>
    {
        using var badHandler = new HttpClientHandler { AllowAutoRedirect = false };
        using var badClient = new HttpClient(badHandler) { BaseAddress = client.BaseAddress };
        badClient.DefaultRequestHeaders.Add("Cookie", "MocBakery.Customer=invalid");
        using var response = await badClient.GetAsync(profile);
        Assert(response.StatusCode == HttpStatusCode.Redirect);
    });
    await Check("GET cannot sign a customer out", async () =>
    {
        using var response = await client.GetAsync(profile + "/dang-xuat");
        Assert(response.StatusCode == HttpStatusCode.MethodNotAllowed);
    });
    await Check("Logout without antiforgery token preserves session", async () =>
    {
        using var response = await client.PostAsync(profile + "/dang-xuat", new FormUrlEncodedContent(new Dictionary<string, string>()));
        using var stillSignedIn = await client.GetAsync(profile);
        Assert(response.StatusCode == HttpStatusCode.BadRequest && stillSignedIn.StatusCode == HttpStatusCode.OK);
    });
    await Check("POST logout removes cookie and protected page becomes inaccessible", async () =>
    {
        using var response = await Post(profile + "/dang-xuat", [], profile);
        using var after = await client.GetAsync(profile);
        Assert(response.StatusCode == HttpStatusCode.Redirect && after.StatusCode == HttpStatusCode.Redirect);
    });
    await Check("Cookie from before password change is revoked on next request", async () =>
    {
        using var response = await Post(login, Login());
        var service = new CustomerAccountService(repository);
        service.ChangePassword(service.Login("web.customer", "WebPassword123!"), "WebPassword123!", "ChangedPassword123!", "ChangedPassword123!");
        using var after = await client.GetAsync(profile);
        Assert(after.StatusCode == HttpStatusCode.Redirect);
    });
    await Check("Disabled account loses its existing Web session", async () =>
    {
        using var response = await Post(login, Login("ChangedPassword123!"));
        repository.Accounts[1] = repository.Accounts[1] with { Active = false };
        using var after = await client.GetAsync(profile);
        Assert(after.StatusCode == HttpStatusCode.Redirect);
        repository.Accounts[1] = repository.Accounts[1] with { Active = true };
    });
    await Check("Database errors return 503 without SQL details or passwords", async () =>
    {
        repository.Unavailable = true;
        using var response = await Post(register, Registration("new.customer", "0900000003"));
        string html = await response.Content.ReadAsStringAsync();
        Assert(response.StatusCode == HttpStatusCode.ServiceUnavailable && !html.Contains("FixtureSecret") && !html.Contains("WebPassword123!"));
        repository.Unavailable = false;
    });
    await Check("Antiforgery token cannot be reused without its browser cookie", async () =>
    {
        string token = Token(await Page(register));
        using var other = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = client.BaseAddress };
        var data = Registration(); data["__RequestVerificationToken"] = token;
        using var response = await other.PostAsync(register, new FormUrlEncodedContent(data));
        Assert(response.StatusCode == HttpStatusCode.BadRequest);
    });
    await Check("Production authentication requires secure cookies with a fixed expiry", async () =>
    {
        await using var production = WebHost.Build(["--contentRoot", root, "--environment", "Production"], builder =>
        {
            builder.Logging.ClearProviders();
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
        var options = production.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        Assert(options.Cookie.SecurePolicy == CookieSecurePolicy.Always && options.Cookie.HttpOnly
            && options.ExpireTimeSpan == TimeSpan.FromMinutes(30) && !options.SlidingExpiration);
    });
    await Check("Login attempts are rate limited", async () =>
    {
        bool blocked = false;
        for (int i = 0; i < 12; i++)
        {
            using var response = await Post(login, Login("IncorrectPassword!"));
            if (response.StatusCode == HttpStatusCode.TooManyRequests) { blocked = true; break; }
        }
        Assert(blocked);
    });
}
finally { await app.StopAsync(); }
var passwordResults = await PasswordChangeChecks.Run(root);
passed += passwordResults.Passed;
failed += passwordResults.Failed;
Console.WriteLine($"WEB ACCOUNT RESULT: {passed} passed, {failed} failed; real HTTP with memory repository, no SQL operations.");
Environment.ExitCode = failed == 0 ? 0 : 1;

sealed class WebFixtureRepository : ICustomerAccountRepository
{
    public Dictionary<int, CustomerAccount> Accounts { get; } = [];
    public Dictionary<int, CustomerProfile> Profiles { get; } = [];
    public bool Unavailable { get; set; }
    public bool RevokeOnNextProfileWrite { get; set; }
    public int ProfileWrites { get; private set; }
    public CustomerAccount? FindByUsername(string username) => Accounts.Values.SingleOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    public CustomerAccount? FindById(int id) => Accounts.GetValueOrDefault(id);
    public int Register(string username, string hash, CustomerProfile profile)
    {
        if (Unavailable) throw new FixtureDbException();
        if (FindByUsername(username) is not null) throw new BusinessException("Tên đăng nhập đã tồn tại.", "Username");
        if (Profiles.Values.Any(p => p.Phone == profile.Phone)) throw new BusinessException("Số điện thoại đã có hồ sơ.", "Phone");
        int id = Accounts.Count + 1;
        Accounts.Add(id, new(id, id, username, hash, true, Guid.NewGuid())); Profiles.Add(id, profile); return id;
    }
    public CustomerProfile? ReadProfile(CustomerIdentity identity) => Profiles.GetValueOrDefault(identity.CustomerId);
    public bool UpdateProfile(CustomerIdentity identity, CustomerProfile profile)
    {
        if (Unavailable) throw new FixtureDbException();
        if (RevokeOnNextProfileWrite)
        {
            RevokeOnNextProfileWrite = false;
            Accounts[identity.AccountId] = Accounts[identity.AccountId] with { SecurityStamp = Guid.NewGuid() };
        }
        var current = Accounts[identity.AccountId];
        if (!current.Active || current.CustomerId != identity.CustomerId || current.SecurityStamp != identity.SecurityStamp) return false;
        if (Profiles.Any(p => p.Key != identity.CustomerId && p.Value.Phone == profile.Phone))
            throw new BusinessException("Số điện thoại đã có hồ sơ khác.", "Phone");
        Profiles[identity.CustomerId] = profile; ProfileWrites++; return true;
    }
    public bool ChangePassword(CustomerIdentity identity, string expectedHash, string nextHash)
    {
        if (Unavailable) throw new FixtureDbException();
        var current = Accounts[identity.AccountId];
        if (current.PasswordHash != expectedHash || current.SecurityStamp != identity.SecurityStamp) return false;
        Accounts[current.Id] = current with { PasswordHash = nextHash, SecurityStamp = Guid.NewGuid() }; return true;
    }
}
sealed class FixtureDbException() : DbException("FixtureSecret: server and database details must not appear in HTML");
