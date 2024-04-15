using System.Security.Cryptography;
using DropBear.Codex.Files.Encryption;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Strategies;

public class AesGcmEncryptionStrategy : IEncryptionStrategy
{
    private readonly AesGcmEncryptor _encryptor;

    public AesGcmEncryptionStrategy(RSA rsa) => _encryptor = new AesGcmEncryptor(rsa);

    public byte[] ProcessData(byte[] data) => _encryptor.Encrypt(data);

    public byte[] RevertData(byte[] data) => _encryptor.Decrypt(data);

    ~AesGcmEncryptionStrategy() => _encryptor.Dispose();
}
