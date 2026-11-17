using System.Text.Json;
using MocBakery.GUI.Forms.Accounts;

internal static class LoginPreferencesChecks
{
    internal static void Run(Action<string, Action> check, Action<bool> assert)
    {
        string root = Path.Combine(Path.GetTempPath(), "MocBakeryPreferences-" + Guid.NewGuid().ToString("N"));
        var store = new RememberedUsernameStore(root);
        try
        {
            check("Missing username preference does not create a file", () =>
            {
                assert(store.Load() == "" && !Directory.Exists(root));
            });
            check("Remember username creates preference directory and roundtrips Unicode", () =>
            {
                store.Save("  thái.nguyễn  ", true);
                assert(store.Load() == "thái.nguyễn");
            });
            check("Username preference serializes only a string", () =>
            {
                using var json = JsonDocument.Parse(File.ReadAllText(store.FilePath));
                assert(json.RootElement.ValueKind == JsonValueKind.String && json.RootElement.GetString() == "thái.nguyễn");
            });
            check("Saving a different username replaces remembered username", () =>
            {
                store.Save("second.user", true);
                assert(store.Load() == "second.user");
            });
            check("Unchecking remember removes preference and is repeatable", () =>
            {
                store.Save("second.user", false);
                store.Save("second.user", false);
                assert(!File.Exists(store.FilePath) && store.Load() == "");
            });
            check("Malformed preference surfaces JSON error for login error handling", () =>
            {
                File.WriteAllText(store.FilePath, "{invalid");
                try { store.Load(); }
                catch (JsonException) { return; }
                throw new Exception("Expected malformed preference to be rejected.");
            });
        }
        finally
        {
            // Remove only the exact file and empty directory created by this test.
            if (File.Exists(store.FilePath)) File.Delete(store.FilePath);
            if (Directory.Exists(root)) Directory.Delete(root);
        }
    }
}
