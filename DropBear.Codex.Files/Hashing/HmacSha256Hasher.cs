using System.Security.Cryptography;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Hashing;

public class HmacSha256Hasher : IHasher, IDisposable
{
    private readonly RSA _rsa;

    public HmacSha256Hasher(RSA rsa)
    {
        _rsa = rsa;
    }

    public void Dispose()
    {
        // Dispose logic if needed, for RSA keys if they're not managed elsewhere
        _rsa.Dispose();
    }

    public string Hash(byte[] data)
    {
        using var hmac = new HMACSHA256();
        // Generate a random key for HMAC
        hmac.Key = GenerateRandomKey(32); // 32 bytes for SHA-256

        // Compute the HMAC
        var hashBytes = hmac.ComputeHash(data);

        // Encrypt the HMAC key with RSA
        var encryptedKey = _rsa.Encrypt(hmac.Key, RSAEncryptionPadding.OaepSHA256);

        // Convert hash and encrypted key to Base64 and combine them
        var base64Hash = Convert.ToBase64String(hashBytes);
        var base64EncryptedKey = Convert.ToBase64String(encryptedKey);
        return $"{base64Hash}:{base64EncryptedKey}";
    }

    public bool VerifyHash(byte[] data, string hash)
    {
        var parts = hash.Split(':');
        if (parts.Length is not 2) return false;

        var expectedHash = Convert.FromBase64String(parts[0]);
        var encryptedKey = Convert.FromBase64String(parts[1]);

        // Decrypt the HMAC key
        var hmacKey = _rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);

        using var hmac = new HMACSHA256(hmacKey);
        var computedHash = hmac.ComputeHash(data);
        return AreHashesEqual(computedHash, expectedHash);
    }

    private static byte[] GenerateRandomKey(int size)
    {
        var randomKey = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomKey);

        return randomKey;
    }

    private static bool AreHashesEqual(IReadOnlyCollection<byte> first, IReadOnlyList<byte> second)
    {
        if (first.Count != second.Count) return false;
        return !first.Where((t, i) => t != second[i]).Any();
    }
    
}
