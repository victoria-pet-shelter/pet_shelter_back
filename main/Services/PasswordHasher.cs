using System.Security.Cryptography;
using System.Text;

namespace Security;

public static class PasswordHasher
{
    // Хэширует пароль с использованием PBKDF2
    public static string Hash(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] salt = new byte[16];
        rng.GetBytes(salt);

        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);

        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    // Проверяет, соответствует ли введённый пароль хэшу
    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] originalHash = Convert.FromBase64String(parts[1]);

        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] testHash = pbkdf2.GetBytes(32);

        return testHash.SequenceEqual(originalHash);
    }
}
