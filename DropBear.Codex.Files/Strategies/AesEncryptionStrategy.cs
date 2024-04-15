using System.Security.Cryptography;
using DropBear.Codex.Files.Encryption;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Strategies;

public class AesEncryptionStrategy : IEncryptionStrategy
{
    private readonly AesEncryptor _encryptor;

    public AesEncryptionStrategy(RSA rsa) => _encryptor = new AesEncryptor(rsa);

    public byte[] ProcessData(byte[] data) => _encryptor.Encrypt(data);

    public byte[] RevertData(byte[] data) => _encryptor.Decrypt(data);

    ~AesEncryptionStrategy() => _encryptor.Dispose();
}
