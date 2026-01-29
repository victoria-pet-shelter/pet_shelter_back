using System.Security.Cryptography;
using System.Text;

public static class EncryptionService
{
    private static readonly string RawKey =
        Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
        ?? "a3G!t8ZkL2#vN9@eQ4XpB1mR7wDfT6Hs";

    private static byte[] GetKeyBytes()
    {
        // Берём строку и делаем из неё 32-байтный ключ через SHA256
        var rawBytes = Encoding.UTF8.GetBytes(RawKey);
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(rawBytes); // ВСЕГДА 32 байта
    }

    public static string? Encrypt(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return null;

        using var aes = Aes.Create();
        aes.Key = GetKeyBytes();   // гарантированно валидный размер
        aes.GenerateIV();          // 16 байт по умолчанию

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();

        // Сначала кладём IV
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public static string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return null;

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = GetKeyBytes();  // тот же самый ключ

        var iv = new byte[16];
        Array.Copy(fullCipher, iv, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }

    public static string Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input.ToLowerInvariant());
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }
}