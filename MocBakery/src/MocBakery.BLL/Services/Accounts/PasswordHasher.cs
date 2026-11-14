using System.Security.Cryptography;
namespace MocBakery.BLL.Services.Accounts;
public static class PasswordHasher
{
    private const int Iterations = 210000;
    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
    public static bool Verify(string password, string stored)
    {
        try
        {
            string[] parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2-SHA256" ||
                !int.TryParse(parts[1], out int count) || count < 100000 || count > 1000000) return false;
            byte[] salt = Convert.FromBase64String(parts[2]), expected = Convert.FromBase64String(parts[3]);
            if (salt.Length != 16 || expected.Length != 32) return false;
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, count, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }
}
