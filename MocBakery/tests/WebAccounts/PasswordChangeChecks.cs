using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.CustomerAccounts;
using MocBakery.Models.CustomerAccounts;
using WebHost = MocBakery.Web.Program;

internal static class PasswordChangeChecks
{
    internal static async Task<(int Passed, int Failed)> Run(string root)
    {
        const string startPassword = "StartPassword123!", nextPassword = "ChangedPassword123!";
        const string login = "/tai-khoan/dang-nhap", profile = "/tai-khoan", change = "/tai-khoan/doi-mat-khau";
        var repository = new WebFixtureRepository();
        var accounts = new CustomerAccountService(repository);
        accounts.Register(new("password.customer", startPassword, startPassword, "Khách đổi mật khẩu", "0901111111", null));
        accounts.Register(new("other.customer", startPassword, startPassword, "Khách khác", "0902222222", null));
        await using var app = WebHost.Build(["--contentRoot", root, "--environment", "Development"], builder =>
        {
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            builder.Services.AddSingleton<ICustomerAccountRepository>(repository);
        });
        await app.StartAsync();
        var address = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
        using var first = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = address };
        using var second = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = address };
        int passed = 0, failed = 0;
        static void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
        async Task Check(string title, Func<Task> test)
        {
            try { await test(); passed++; Console.WriteLine("PASS " + title); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + title + ": " + ex.Message); }
        }
        async Task<HttpResponseMessage> Post(HttpClient client, string path, Dictionary<string, string> values)
        {
            string html = await client.GetStringAsync(path);
            string token = WebUtility.HtmlDecode(Regex.Match(html,
                "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            Assert(token.Length > 20);
            values["__RequestVerificationToken"] = token;
            return await client.PostAsync(path, new FormUrlEncodedContent(values));
        }
        static Dictionary<string, string> Form(string oldPassword = startPassword, string password = nextPassword, string confirmation = nextPassword) =>
            new() { ["OldPassword"] = oldPassword, ["Password"] = password, ["Confirmation"] = confirmation };
        static Dictionary<string, string> Credentials(string password = startPassword) =>
            new() { ["Username"] = "password.customer", ["Password"] = password };
        static void AssertNoPasswords(string html, params string[] values)
        {
            Assert(!Regex.IsMatch(html, "type=\"password\"[^>]*value=\"[^\"]+\""));
            foreach (string value in values.Where(v => v.Length > 0)) Assert(!html.Contains(value));
        }
        try
        {
            using (var signedIn = await Post(first, login, Credentials())) Assert(signedIn.StatusCode == HttpStatusCode.Redirect);
            using (var anotherDevice = await Post(second, login, Credentials())) Assert(anotherDevice.StatusCode == HttpStatusCode.Redirect);
            await Check("Anonymous visitors cannot open or submit password changes", async () =>
            {
                using var anonymous = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = address };
                using var get = await anonymous.GetAsync(change);
                using var post = await anonymous.PostAsync(change, new FormUrlEncodedContent(Form()));
                Assert(get.StatusCode == HttpStatusCode.Redirect && post.StatusCode == HttpStatusCode.Redirect);
            });
            await Check("Password page shows three empty masked fields and a real antiforgery token", async () =>
            {
                string before = repository.Accounts[1].PasswordHash;
                string html = await first.GetStringAsync(change);
                Assert(Regex.Matches(html, "type=\"password\"").Count == 3 && html.Contains("__RequestVerificationToken")
                    && (await first.GetStringAsync(profile)).Contains("doi-mat-khau") && repository.Accounts[1].PasswordHash == before);
                AssertNoPasswords(html, startPassword, nextPassword);
            });
            await Check("Password POST without antiforgery token does not change credentials", async () =>
            {
                var before = repository.Accounts[1];
                using var response = await first.PostAsync(change, new FormUrlEncodedContent(Form()));
                Assert(response.StatusCode == HttpStatusCode.BadRequest && repository.Accounts[1] == before);
            });
            await Check("Password page validates all required fields server side", async () =>
            {
                var before = repository.Accounts[1];
                using var response = await Post(first, change, Form("", "", ""));
                string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
                Assert(response.StatusCode == HttpStatusCode.OK && html.Contains("Nhập mật khẩu hiện tại")
                    && html.Contains("Nhập mật khẩu mới") && html.Contains("Nhập xác nhận mật khẩu") && repository.Accounts[1] == before);
            });
            await Check("Web password limits reject 7 and 65 characters", async () =>
            {
                var before = repository.Accounts[1];
                foreach (int length in new[] { 7, 65 })
                {
                    string value = new('x', length);
                    using var response = await Post(first, change, Form(startPassword, value, value));
                    string html = await response.Content.ReadAsStringAsync();
                    Assert(response.StatusCode == HttpStatusCode.OK && html.Contains("field-validation-error") && repository.Accounts[1] == before);
                    AssertNoPasswords(html, startPassword, value);
                }
            });
            await Check("Confirmation mismatch does not change the password or echo secrets", async () =>
            {
                var before = repository.Accounts[1];
                using var response = await Post(first, change, Form(startPassword, nextPassword, "DifferentPassword123!"));
                string html = await response.Content.ReadAsStringAsync();
                Assert(response.StatusCode == HttpStatusCode.OK && WebUtility.HtmlDecode(html).Contains("Xác nhận mật khẩu không khớp") && repository.Accounts[1] == before);
                AssertNoPasswords(html, startPassword, nextPassword, "DifferentPassword123!");
            });
            await Check("Wrong current password leaves all credentials unchanged", async () =>
            {
                var before = repository.Accounts[1];
                using var response = await Post(first, change, Form("WrongCurrentPassword!"));
                string html = await response.Content.ReadAsStringAsync();
                Assert(response.StatusCode == HttpStatusCode.OK && WebUtility.HtmlDecode(html).Contains("Mật khẩu hiện tại không đúng") && repository.Accounts[1] == before);
                AssertNoPasswords(html, "WrongCurrentPassword!", nextPassword);
            });
            await Check("Reusing the current password produces a field error", async () =>
            {
                var before = repository.Accounts[1];
                using var response = await Post(first, change, Form(startPassword, startPassword, startPassword));
                string html = await response.Content.ReadAsStringAsync();
                Assert(response.StatusCode == HttpStatusCode.OK && html.Contains("field-validation-error") && repository.Accounts[1] == before);
                AssertNoPasswords(html, startPassword);
            });
            await Check("Password database failure returns 503 without modifying credentials", async () =>
            {
                var before = repository.Accounts[1];
                repository.Unavailable = true;
                try
                {
                    using var response = await Post(first, change, Form());
                    string html = await response.Content.ReadAsStringAsync();
                    Assert(response.StatusCode == HttpStatusCode.ServiceUnavailable && repository.Accounts[1] == before && !html.Contains("FixtureSecret"));
                    AssertNoPasswords(html, startPassword, nextPassword);
                }
                finally { repository.Unavailable = false; }
            });
            await Check("Successful Web password change affects only the authenticated account", async () =>
            {
                var original = repository.Accounts[1];
                var other = repository.Accounts[2];
                var data = Form(); data["AccountId"] = "2"; data["CustomerId"] = "2"; data["Role"] = "ChuTiem";
                using var response = await Post(first, change, data);
                using var protectedPage = await first.GetAsync(profile);
                Assert(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.ToString() == login
                    && protectedPage.StatusCode == HttpStatusCode.Redirect && repository.Accounts[2] == other
                    && repository.Accounts[1].SecurityStamp != original.SecurityStamp
                    && PasswordHasher.Verify(nextPassword, repository.Accounts[1].PasswordHash));
            });
            await Check("Password change invalidates the cookie on another browser", async () =>
            {
                using var response = await second.GetAsync(profile);
                Assert(response.StatusCode == HttpStatusCode.Redirect);
            });
            await Check("Password success message is displayed once at login", async () =>
            {
                string html = WebUtility.HtmlDecode(await first.GetStringAsync(login));
                string refresh = WebUtility.HtmlDecode(await first.GetStringAsync(login));
                Assert(html.Contains("Đổi mật khẩu thành công") && !refresh.Contains("Đổi mật khẩu thành công"));
            });
            await Check("Old password is rejected after the Web change", async () =>
            {
                using var response = await Post(first, login, Credentials());
                using var profileResponse = await first.GetAsync(profile);
                Assert(response.StatusCode == HttpStatusCode.OK && profileResponse.StatusCode == HttpStatusCode.Redirect);
            });
            await Check("New password signs in to the same customer account", async () =>
            {
                using var response = await Post(first, login, Credentials(nextPassword));
                string html = WebUtility.HtmlDecode(await first.GetStringAsync(profile));
                Assert(response.StatusCode == HttpStatusCode.Redirect && html.Contains("0901111111") && !html.Contains("0902222222"));
            });
            await Check("Password attempts are rate limited without changing the saved hash", async () =>
            {
                var before = repository.Accounts[1];
                bool blocked = false;
                for (int i = 0; i < 12; i++)
                {
                    using var response = await Post(first, change, Form("WrongCurrentPassword!"));
                    if (response.StatusCode == HttpStatusCode.TooManyRequests) { blocked = true; break; }
                }
                Assert(blocked && repository.Accounts[1] == before);
            });
        }
        finally { await app.StopAsync(); }
        return (passed, failed);
    }
}
