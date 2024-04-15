using System.Security.Cryptography;
using System.Text;
using DropBear.Codex.Files.Hashing;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Strategies;

public class HmacSha256HashingStrategy : IHashingStrategy
{
    private readonly HmacSha256Hasher _hasher;

    public HmacSha256HashingStrategy(RSA rsa) => _hasher = new HmacSha256Hasher(rsa);

    public byte[] ProcessData(byte[] data)
    {
        // Compute and return the HMAC hash
        var hash = _hasher.Hash(data);
        return Encoding.UTF8.GetBytes(hash);
    }

    public byte[] RevertData(byte[] data) =>
        // Similar to SHA256, this would typically return the data as HMAC is not reversible
        data;

    ~HmacSha256HashingStrategy() => _hasher.Dispose();
}
