using System.Text;
using DropBear.Codex.Files.Hashing;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Strategies;

public class SHA256HashingStrategy : IHashingStrategy
{
    private readonly SHA256Hasher _hasher;

    public SHA256HashingStrategy() => _hasher = new SHA256Hasher();

    public byte[] ProcessData(byte[] data)
    {
        // Hash data and encode the hash as byte array
        var hash = _hasher.Hash(data);
        return Encoding.UTF8.GetBytes(hash);
    }

    public byte[] RevertData(byte[] data) =>
        // This method might simply return the data as hashing is generally not reversible
        data;
}
