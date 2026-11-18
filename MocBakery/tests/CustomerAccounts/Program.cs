using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.CustomerAccounts;
using MocBakery.Models;
using MocBakery.Models.CustomerAccounts;

int passed = 0, failed = 0;
void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
void Check(string name, Action test)
{
    try { test(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
}
void Reject(Action test, string? field = null)
{
    try { test(); }
    catch (BusinessException ex) { Assert(field is null || field == ex.Field); return; }
    throw new Exception("Expected BusinessException");
}
CustomerRegistration Input(string username = "customer.one", string phone = "0901234567") =>
    new(username, "Password123!", "Password123!", "  Nguyễn   An  ", phone, " TP.HCM ");
var repo = new MemoryCustomerAccounts();
var service = new CustomerAccountService(repo);
Check("Registration normalizes login and shared profile", () =>
{
    Assert(service.Register(Input(" CUSTOMER.ONE ")) == 1);
    Assert(repo.Accounts[1].Username == "customer.one" && repo.Profiles[1] == new CustomerProfile("Nguyễn An", "0901234567", "TP.HCM"));
});
Check("Registration stores a salted password hash", () =>
    Assert(repo.Accounts[1].PasswordHash != "Password123!" && PasswordHasher.Verify("Password123!", repo.Accounts[1].PasswordHash)));
Check("Duplicate username rejected case insensitively", () => Reject(() => service.Register(Input("CUSTOMER.ONE", "0900000002")), "Username"));
Check("Registration does not claim an existing phone", () => Reject(() => service.Register(Input("customer.two")), "Phone"));
foreach (string username in new[] { "ab", new string('a', 51), "khách", "a b", "a\nb", ".abc" })
    Check("Reject invalid username " + username.Replace("\n", "[newline]"), () => Reject(() => service.Register(Input(username)), "Username"));
foreach (int length in new[] { 7, 65 })
    Check("Reject password boundary " + length, () => Reject(() => service.Register(Input() with { Password = new('x', length), Confirmation = new('x', length) }), "Password"));
Check("Reject confirmation mismatch", () => Reject(() => service.Register(Input() with { Confirmation = "different" }), "Confirmation"));
Check("Reject blank name", () => Reject(() => service.Register(Input() with { FullName = " " }), "FullName"));
Check("Reject name above 100 characters", () => Reject(() => service.Register(Input() with { FullName = new('n', 101) }), "FullName"));
Check("Reject address above 255 characters", () => Reject(() => service.Register(Input() with { Address = new('a', 256) }), "Address"));
foreach (string phone in new[] { "123", "09012345678", "090123456\n", "０９０１２３４５６７" })
    Check("Reject malformed phone", () => Reject(() => service.Register(Input() with { Phone = phone }), "Phone"));
CustomerIdentity identity = service.Login(" CUSTOMER.ONE ", "Password123!");
Check("Login returns only customer identity", () => Assert(identity.AccountId == 1 && identity.CustomerId == 1 && identity.SecurityStamp != Guid.Empty));
Check("Wrong password rejected", () => Reject(() => service.Login("customer.one", "Incorrect123!")));
Check("Unknown account rejected", () => Reject(() => service.Login("unknown", "Password123!")));
Check("Shared profile contains no internal notes or employee role", () =>
    Assert(typeof(CustomerProfile).GetProperties().Select(p => p.Name).SequenceEqual(new[] { "FullName", "Phone", "Address" })));
Check("Forged customer id cannot read another profile", () => Reject(() => service.Profile(identity with { CustomerId = 2 })));
Check("Forged security stamp cannot update profile", () => Reject(() => service.UpdateProfile(identity with { SecurityStamp = Guid.NewGuid() }, new("Bad", "0900000002", null))));
Check("Profile update normalizes values and accepts maximum lengths", () =>
{
    service.UpdateProfile(identity, new(new('n', 100), "0901234567", new('a', 255)));
    Assert(service.Profile(identity).FullName.Length == 100 && service.Profile(identity).Address!.Length == 255);
});
Check("Wrong current password preserves stored hash", () =>
{
    string before = repo.Accounts[1].PasswordHash;
    Reject(() => service.ChangePassword(identity, "Incorrect123!", "NewPassword123!", "NewPassword123!"), "OldPassword");
    Assert(before == repo.Accounts[1].PasswordHash);
});
Check("Password change rejects same password", () => Reject(() => service.ChangePassword(identity, "Password123!", "Password123!", "Password123!"), "Password"));
Check("Password change rejects confirmation mismatch", () => Reject(() => service.ChangePassword(identity, "Password123!", "NewPassword123!", "different"), "Confirmation"));
Check("Password change invalidates all previous identities", () =>
{
    var second = service.Login("customer.one", "Password123!");
    service.ChangePassword(identity, "Password123!", "NewPassword123!", "NewPassword123!");
    Reject(() => service.Profile(identity)); Reject(() => service.Profile(second));
    Reject(() => service.Login("customer.one", "Password123!"));
    identity = service.Login("customer.one", "NewPassword123!");
});
Check("Locked customer cannot login or keep an existing identity", () =>
{
    repo.Accounts[1] = repo.Accounts[1] with { Active = false };
    Reject(() => service.Login("customer.one", "NewPassword123!"));
    Reject(() => service.Profile(identity));
    repo.Accounts[1] = repo.Accounts[1] with { Active = true };
});
Check("Concurrent profile change failure is surfaced", () =>
{
    repo.FailWrite = true;
    Reject(() => service.UpdateProfile(identity, new("An", "0901234567", null)));
    repo.FailWrite = false;
});
Check("Concurrent password change failure is surfaced", () =>
{
    repo.FailWrite = true;
    Reject(() => service.ChangePassword(identity, "NewPassword123!", "NextPassword123!", "NextPassword123!"));
    repo.FailWrite = false;
});
Check("Accept password length 8 with optional empty address", () =>
{
    service.Register(Input("boundary.eight", "0900000008") with { Password = "abcdefgh", Confirmation = "abcdefgh", Address = " " });
    Assert(service.Profile(service.Login("boundary.eight", "abcdefgh")).Address is null);
});
Check("Accept password length 64", () =>
{
    string password = new('p', 64);
    service.Register(Input("boundary.sixtyfour", "0900000064") with { Password = password, Confirmation = password });
    Assert(service.Login("boundary.sixtyfour", password).AccountId > 0);
});
Console.WriteLine($"CUSTOMER ACCOUNT RESULT: {passed} passed, {failed} failed; memory repository only.");
Environment.ExitCode = failed == 0 ? 0 : 1;

sealed class MemoryCustomerAccounts : ICustomerAccountRepository
{
    public Dictionary<int, CustomerAccount> Accounts { get; } = [];
    public Dictionary<int, CustomerProfile> Profiles { get; } = [];
    public bool FailWrite { get; set; }
    public CustomerAccount? FindByUsername(string username) => Accounts.Values.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    public CustomerAccount? FindById(int id) => Accounts.GetValueOrDefault(id);
    public int Register(string username, string hash, CustomerProfile profile)
    {
        if (FindByUsername(username) is not null) throw new BusinessException("Duplicate username", "Username");
        if (Profiles.Values.Any(p => p.Phone == profile.Phone)) throw new BusinessException("Existing phone", "Phone");
        int id = Accounts.Count + 1;
        Accounts.Add(id, new(id, id, username, hash, true, Guid.NewGuid())); Profiles.Add(id, profile);
        return id;
    }
    private bool Valid(CustomerIdentity identity) => Accounts.TryGetValue(identity.AccountId, out var a) && a.Active && a.CustomerId == identity.CustomerId && a.SecurityStamp == identity.SecurityStamp;
    public CustomerProfile? ReadProfile(CustomerIdentity identity) => Valid(identity) ? Profiles[identity.CustomerId] : null;
    public bool UpdateProfile(CustomerIdentity identity, CustomerProfile profile)
    {
        if (FailWrite || !Valid(identity)) return false;
        Profiles[identity.CustomerId] = profile; return true;
    }
    public bool ChangePassword(CustomerIdentity identity, string expectedHash, string nextHash)
    {
        if (FailWrite || !Valid(identity) || Accounts[identity.AccountId].PasswordHash != expectedHash) return false;
        Accounts[identity.AccountId] = Accounts[identity.AccountId] with { PasswordHash = nextHash, SecurityStamp = Guid.NewGuid() };
        return true;
    }
}
