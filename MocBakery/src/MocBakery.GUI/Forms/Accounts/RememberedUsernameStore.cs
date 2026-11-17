using System.Text.Json;

namespace MocBakery.GUI.Forms.Accounts;

// This preference contains only a username, never a password or session token.
internal sealed class RememberedUsernameStore(string directory)
{
    internal string FilePath { get; } = Path.Combine(directory, "username.json");

    internal static RememberedUsernameStore ForCurrentUser() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MocBakery"));

    internal string Load()
    {
        if (!File.Exists(FilePath)) return "";
        return (JsonSerializer.Deserialize<string>(File.ReadAllText(FilePath)) ?? "").Trim();
    }

    internal void Save(string username, bool remember)
    {
        if (!remember)
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            return;
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(username.Trim()));
    }
}
