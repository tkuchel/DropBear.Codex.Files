using System.Security.Cryptography;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Hashing;

// ReSharper disable once InconsistentNaming
public class SHA256Hasher : IHasher
{
    public string Hash(byte[] data)
    {
        return Convert.ToBase64String(SHA256.HashData(data));
    }
}
