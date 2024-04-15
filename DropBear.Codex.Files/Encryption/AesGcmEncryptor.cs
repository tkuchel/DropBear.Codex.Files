using System.Security.Cryptography;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Encryption;

public class AesGcmEncryptor : IEncryptor, IDisposable
{
    private const int KeySize = 32; // AES-256 key size in bytes
    private readonly RSA _rsa;
    private byte[] _key = [];

    public AesGcmEncryptor(RSA rsa)
    {
        _rsa = rsa;
        GenerateKey();
    }

    public void Dispose()
    {
        // Add any necessary cleanup code here
        _rsa.Dispose();
    }

    public byte[] Encrypt(byte[] data)
    {
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);
        var ciphertext = new byte[data.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using (var aesGcm = new AesGcm(_key, 16))
        {
            aesGcm.Encrypt(nonce, data, ciphertext, tag);
        }

        // Encrypt the key and nonce using RSA
        var encryptedKey = _rsa.Encrypt(_key, RSAEncryptionPadding.OaepSHA256);
        var encryptedNonce = _rsa.Encrypt(nonce, RSAEncryptionPadding.OaepSHA256);

        return Combine(encryptedKey, encryptedNonce, tag, ciphertext);
    }

    public byte[] Decrypt(byte[] data)
    {
        var encryptedKey = new byte[_rsa.KeySize / 8];
        var encryptedNonce = new byte[_rsa.KeySize / 8];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var ciphertext = new byte[data.Length - encryptedKey.Length - encryptedNonce.Length - tag.Length];

        Buffer.BlockCopy(data, 0, encryptedKey, 0, encryptedKey.Length);
        Buffer.BlockCopy(data, encryptedKey.Length, encryptedNonce, 0, encryptedNonce.Length);
        Buffer.BlockCopy(data, encryptedKey.Length + encryptedNonce.Length, tag, 0, tag.Length);
        Buffer.BlockCopy(data, encryptedKey.Length + encryptedNonce.Length + tag.Length, ciphertext, 0,
            ciphertext.Length);

        _key = _rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);
        var nonce = _rsa.Decrypt(encryptedNonce, RSAEncryptionPadding.OaepSHA256);

        var plaintext = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(_key, 16);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    private void GenerateKey()
    {
        _key = new byte[KeySize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(_key);
    }

    private static byte[] Combine(params byte[][] arrays)
    {
        var combined = new byte[arrays.Sum(a => a.Length)];
        var offset = 0;
        foreach (var array in arrays)
        {
            Buffer.BlockCopy(array, 0, combined, offset, array.Length);
            offset += array.Length;
        }

        return combined;
    }
}
