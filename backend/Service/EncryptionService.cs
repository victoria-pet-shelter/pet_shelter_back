using System.Security.Cryptography;
using System.Text;

public static class EncryptionService
{
    // Get the encryption key from environment or fallback to hardcoded (should be avoided in production)
    private static readonly string Key = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "a3G!t8ZkL2#vN9@eQ4XpB1mR7wDfT6Hs";

    // Encrypts a plain text string using AES
    public static string? Encrypt(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return null;

        using var aes = Aes.Create(); // Create new AES instance
        aes.Key = Encoding.UTF8.GetBytes(Key);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(); // Memory stream to hold encrypted data
        ms.Write(aes.IV, 0, aes.IV.Length); // Write IV to beginning of stream
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs); // Write plain text into encrypted stream
        sw.Write(plainText);
        sw.Flush();
        cs.FlushFinalBlock();

        return Convert.ToBase64String(ms.ToArray()); // Return encrypted data as Base64 string
    }

    // Decrypts an encrypted Base64 string using AES
    public static string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return null;

        var fullCipher = Convert.FromBase64String(cipherText); // Decode from Base64

        using var aes = Aes.Create(); // Create AES instance
        aes.Key = Encoding.UTF8.GetBytes(Key); // Set key

        var iv = new byte[16]; // Allocate space for IV (16 bytes)
        Array.Copy(fullCipher, iv, 16); // Copy IV from beginning of encrypted data
        aes.IV = iv; // Set IV

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV); // Create decryptor
        using var ms = new MemoryStream(fullCipher, 16, fullCipher.Length - 16); // Stream without IV
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read); // Decrypt stream
        using var sr = new StreamReader(cs); // Read decrypted data
        return sr.ReadToEnd(); // Return plain text
    }

    // Returns a SHA256 hash of the input string (used for lookup without revealing original)
    public static string Hash(string input)
    {
        using var sha256 = SHA256.Create(); // Create SHA256 instance
        var bytes = Encoding.UTF8.GetBytes(input.ToLowerInvariant()); // Convert to lower and encode
        var hashBytes = sha256.ComputeHash(bytes); // Get hash bytes
        return Convert.ToBase64String(hashBytes); // Return hash as Base64
    }
}
