using System.Data.Common;
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
using MocBakery.DAL;
using MocBakery.DAL.Repositories.CustomerAccounts;
using MocBakery.DAL.Repositories.Customers;
using MocBakery.Models;
using MocBakery.Models.CustomerAccounts;
using WebHost = MocBakery.Web.Program;

internal static class SqlAccountChecks
{
    internal static async Task<int> Run()
    {
        string? connection = Environment.GetEnvironmentVariable("MOCBAKERY_SQL_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
        {
            Console.WriteLine("SQL TEST ERROR: Set MOCBAKERY_SQL_TEST_CONNECTION to a test server with CREATE DATABASE permission. No SQL was executed.");
            return 1;
        }
        // Never accept an existing database as the test target. Only drop the GUID database created here.
        string name = "MocBakery_CustomerTest_" + Guid.NewGuid().ToString("N");
        var builder = new DbConnectionStringBuilder { ConnectionString = connection };
        builder.Remove("Database");
        builder["Initial Catalog"] = "master";
        builder["Pooling"] = false;
        var master = new DbConnectionFactory(builder.ConnectionString);
        bool created = false;
        int passed = 0, failed = 0;
        void Assert(bool valid) { if (!valid) throw new Exception("Assertion failed"); }
        void Reject(Action action, string? field = null)
        {
            try { action(); }
            catch (BusinessException ex) when (field is null || ex.Field == field) { return; }
            throw new Exception("Expected business rejection");
        }
        async Task Check(string title, Func<Task> action)
        {
            try { await action(); passed++; Console.WriteLine("PASS SQL " + title); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL SQL " + title + ": " + ex.GetType().Name); }
        }
        try
        {
            using (var c = master.Open()) Execute(c, $"CREATE DATABASE [{name}] COLLATE Vietnamese_CI_AS");
            created = true;
            Console.WriteLine("SQL TEST DATABASE: " + name);
            builder["Initial Catalog"] = name;
            var db = new DbConnectionFactory(builder.ConnectionString);
            string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/MocBakery.Web"));
            string schema = Path.GetFullPath(Path.Combine(root, "../../database/schema"));
            void Migrate(string file)
            {
                using var c = db.Open();
                string sql = File.ReadAllText(Path.Combine(schema, file)).Replace("QuanLyTiemBanh", name, StringComparison.Ordinal);
                foreach (string batch in Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                    if (!string.IsNullOrWhiteSpace(batch)) Execute(c, batch);
            }
            Migrate("01_CreateDatabase.sql");
            Migrate("02_Upgrade_ExcelDesign.sql");
            Migrate("03_Upgrade_StaffAccounts.sql");
            Migrate("04_CustomerWebAccounts.sql");
            var repository = new CustomerAccountRepository(db);
            var accounts = new CustomerAccountService(repository);
            var desktop = new KhachHangRepository(db);
            const string password = "SqlTestPassword123!", next = "SqlChangedPassword123!";
            int Count(string table)
            {
                using var c = db.Open(); using var q = c.CreateCommand();
                q.CommandText = "SELECT COUNT(*) FROM dbo." + table;
                return Convert.ToInt32(q.ExecuteScalar());
            }
            await Check("migration 04 can be repeated without changing customer data", () =>
            {
                int before = Count("KhachHang"); Migrate("04_CustomerWebAccounts.sql");
                Assert(Count("KhachHang") == before && Count("TaiKhoanKhachHang") == 0);
                return Task.CompletedTask;
            });
            await using var app = WebHost.Build(["--contentRoot", root, "--environment", "Development"], web =>
            {
                web.WebHost.UseUrls("http://127.0.0.1:0");
                web.Logging.ClearProviders();
                web.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
                web.Services.AddSingleton(db); // Real SQL repository and service registrations stay intact.
            });
            await app.StartAsync();
            try
            {
                var address = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
                using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = address };
                async Task<HttpResponseMessage> Post(string path, Dictionary<string, string> values)
                {
                    string html = await client.GetStringAsync(path);
                    string token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
                    Assert(token.Length > 20);
                    values["__RequestVerificationToken"] = token;
                    return await client.PostAsync(path, new FormUrlEncodedContent(values));
                }
                const string register = "/tai-khoan/dang-ky", login = "/tai-khoan/dang-nhap", profile = "/tai-khoan";
                Dictionary<string, string> Registration(string username = "sql.customer", string phone = "0901111111") => new()
                {
                    ["Username"] = username, ["Password"] = password, ["Confirmation"] = password,
                    ["FullName"] = "Khách SQL ' % _ [", ["Phone"] = phone, ["Address"] = "Địa chỉ Việt Nam"
                };
                await Check("HTTP signup persists hashed credentials and a profile visible to desktop DAL", async () =>
                {
                    using var response = await Post(register, Registration());
                    var account = repository.FindByUsername("sql.customer")!;
                    var customer = desktop.Search("0901111111").Single();
                    Assert(response.StatusCode == HttpStatusCode.Redirect && account.CustomerId == customer.MaKhachHang
                        && customer.HoTen == "Khách SQL ' % _ [" && account.PasswordHash != password
                        && PasswordHasher.Verify(password, account.PasswordHash) && account.SecurityStamp != Guid.Empty);
                });
                await Check("duplicate username signup does not leave an orphan customer", async () =>
                {
                    int before = Count("KhachHang");
                    using var response = await Post(register, Registration("SQL.CUSTOMER", "0903333333"));
                    Assert(response.StatusCode == HttpStatusCode.OK && Count("KhachHang") == before && Count("TaiKhoanKhachHang") == 1);
                });
                await Check("migration repeat preserves an existing account and its linked profile", () =>
                {
                    var before = repository.FindByUsername("sql.customer")!;
                    var customer = desktop.Search("0901111111").Single();
                    Migrate("04_CustomerWebAccounts.sql");
                    Assert(repository.FindById(before.Id) == before && desktop.Search("0901111111").Single() == customer);
                    return Task.CompletedTask;
                });
                await Check("account insert failure rolls back the customer inserted in the same transaction", () =>
                {
                    int before = Count("KhachHang");
                    using var c = db.Open();
                    Execute(c, "CREATE TRIGGER dbo.CustomerTest_RejectInsert ON dbo.TaiKhoanKhachHang AFTER INSERT AS THROW 51001, 'Injected test write failure', 1;");
                    try
                    {
                        bool rejected = false;
                        try { accounts.Register(new("rollback.customer", password, password, "Rollback", "0905555555", null)); }
                        catch (DbException) { rejected = true; }
                        Assert(rejected && Count("KhachHang") == before && desktop.Search("0905555555").Count == 0
                            && repository.FindByUsername("rollback.customer") is null);
                    }
                    finally { Execute(c, "DROP TRIGGER dbo.CustomerTest_RejectInsert"); }
                    return Task.CompletedTask;
                });
                int existingId = desktop.Save(new(0, "Khách desktop", "0902222222", null, "Ghi chú nội bộ"));
                await Check("public signup cannot claim a customer created by desktop", async () =>
                {
                    using var response = await Post(register, Registration("claim.customer", "0902222222"));
                    Assert(response.StatusCode == HttpStatusCode.OK && repository.FindByUsername("claim.customer") is null
                        && desktop.Search("0902222222").Single().MaKhachHang == existingId);
                });
                await Check("HTTP login authenticates against SQL credentials", async () =>
                {
                    using var response = await Post(login, new() { ["Username"] = "sql.customer", ["Password"] = password });
                    Assert(response.StatusCode == HttpStatusCode.Redirect
                        && (await client.GetStringAsync(profile)).Contains("0901111111"));
                });
                var identity = accounts.Login("sql.customer", password);
                await Check("desktop changes are visible on the authenticated Web profile", async () =>
                {
                    var customer = desktop.Search("0901111111").Single();
                    desktop.Save(customer with { HoTen = "Desktop cập nhật", GhiChu = "INTERNAL_SQL_NOTE" });
                    string html = WebUtility.HtmlDecode(await client.GetStringAsync(profile));
                    Assert(html.Contains("Desktop cập nhật") && !html.Contains("INTERNAL_SQL_NOTE"));
                });
                await Check("HTTP profile update changes the same desktop record and preserves internal notes", async () =>
                {
                    using var response = await Post(profile + "/ho-so", new()
                    {
                        ["FullName"] = "Web cập nhật ' % _ [", ["Phone"] = "0901111111", ["Address"] = "Địa chỉ mới",
                        ["CustomerId"] = existingId.ToString(), ["GhiChu"] = "FORGED_NOTE"
                    });
                    var customer = desktop.Search("0901111111").Single();
                    Assert(response.StatusCode == HttpStatusCode.Redirect && customer.MaKhachHang == identity.CustomerId
                        && customer.HoTen == "Web cập nhật ' % _ [" && customer.GhiChu == "INTERNAL_SQL_NOTE"
                        && desktop.Search("0902222222").Single().HoTen == "Khách desktop");
                });
                await Check("SQL unique phone rejection preserves the original profile", () =>
                {
                    var before = accounts.Profile(identity);
                    Reject(() => accounts.UpdateProfile(identity, before with { Phone = "0902222222" }), "Phone");
                    Assert(accounts.Profile(identity) == before); return Task.CompletedTask;
                });
                await Check("forged customer identity cannot read or update another SQL record", () =>
                {
                    var forged = identity with { CustomerId = existingId };
                    Assert(repository.ReadProfile(forged) is null && !repository.UpdateProfile(forged, new("Forged", "0904444444", null)));
                    Reject(() => accounts.Profile(forged)); return Task.CompletedTask;
                });
                await Check("password compare-and-swap rejects an unexpected hash", () =>
                {
                    var before = repository.FindById(identity.AccountId)!;
                    Assert(!repository.ChangePassword(identity, "unexpected-hash", PasswordHasher.Hash(next))
                        && repository.FindById(identity.AccountId) == before); return Task.CompletedTask;
                });
                await Check("HTTP password change rotates SQL stamp and rejects old credentials", async () =>
                {
                    using var response = await Post(profile + "/doi-mat-khau", new()
                    { ["OldPassword"] = password, ["Password"] = next, ["Confirmation"] = next });
                    Assert(response.StatusCode == HttpStatusCode.Redirect
                        && repository.FindById(identity.AccountId)!.SecurityStamp != identity.SecurityStamp);
                    Reject(() => accounts.Login("sql.customer", password));
                    Assert(accounts.Login("sql.customer", next).CustomerId == identity.CustomerId);
                });
                await Check("revoked SQL identity cannot read profile or overwrite credentials", () =>
                {
                    Assert(repository.ReadProfile(identity) is null
                        && !repository.UpdateProfile(identity, new("Stale", "0904444444", null))
                        && !repository.ChangePassword(identity, repository.FindById(identity.AccountId)!.PasswordHash, "stale-hash"));
                    return Task.CompletedTask;
                });
                await Check("customer Web operations create no employee accounts", () =>
                { Assert(Count("TaiKhoan") == 0 && Count("NhanVien") == 0); return Task.CompletedTask; });
                await Check("disabled SQL customer cannot log in or update a profile", () =>
                {
                    var current = accounts.Login("sql.customer", next);
                    using var c = db.Open();
                    Execute(c, "UPDATE dbo.TaiKhoanKhachHang SET TrangThai=0");
                    Reject(() => accounts.Login("sql.customer", next));
                    Assert(repository.ReadProfile(current) is null
                        && !repository.UpdateProfile(current, new("Disabled", "0904444444", null)));
                    return Task.CompletedTask;
                });
            }
            finally { await app.StopAsync(); }
        }
        catch (Exception ex)
        {
            failed++;
            Console.WriteLine("SQL TEST SETUP/EXECUTION ERROR: " + ex.GetType().Name + ". Check test server access, SQL driver and schema; connection details are not printed.");
        }
        finally
        {
            if (created)
            {
                try
                {
                    using var c = master.Open();
                    Execute(c, $"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]");
                    Console.WriteLine("SQL TEST DATABASE REMOVED: " + name);
                }
                catch (Exception ex)
                { failed++; Console.WriteLine("SQL CLEANUP FAILED: " + name + " (" + ex.GetType().Name + "). Remove this generated test database manually."); }
            }
        }
        Console.WriteLine($"SQL ACCOUNT RESULT: {passed} passed, {failed} failed; temporary SQL database, real Web HTTP and desktop DAL.");
        return failed == 0 ? 0 : 1;
    }

    private static void Execute(DbConnection c, string sql)
    {
        using var q = c.CreateCommand(); q.CommandText = sql; q.CommandTimeout = 60; q.ExecuteNonQuery();
    }
}
