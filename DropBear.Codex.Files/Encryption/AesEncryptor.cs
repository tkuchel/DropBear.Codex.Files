using System.Security.Cryptography;
using DropBear.Codex.Files.Interfaces;
using Microsoft.IO;

namespace DropBear.Codex.Files.Encryption;

public class AesEncryptor : IEncryptor, IDisposable
{
    private readonly AesCng _aesCng;
    private readonly RecyclableMemoryStreamManager _memoryStreamManager;
    private readonly RSA _rsa;

    public AesEncryptor(RSA rsa)
    {
        _aesCng = new AesCng();
        _rsa = rsa;
        _memoryStreamManager = new RecyclableMemoryStreamManager();
        InitializeCryptoComponents();
    }

    public void Dispose() => _aesCng.Dispose();

    public byte[] Encrypt(byte[] data)
    {
        // Encrypt AES key and IV using RSA
        var encryptedKey = _rsa.Encrypt(_aesCng.Key, RSAEncryptionPadding.OaepSHA256);
        var encryptedIV = _rsa.Encrypt(_aesCng.IV, RSAEncryptionPadding.OaepSHA256);

        using var encryptor = _aesCng.CreateEncryptor();
        using var resultStream = _memoryStreamManager.GetStream("AesEncryptor-Encrypt");
        using var cryptoStream = new CryptoStream(resultStream, encryptor, CryptoStreamMode.Write);
        cryptoStream.Write(data, 0, data.Length);
        cryptoStream.FlushFinalBlock();

        // Combine the encrypted key, IV, and data
        var encryptedData = resultStream.ToArray();
        return Combine(encryptedKey, encryptedIV, encryptedData);
    }

    public byte[] Decrypt(byte[] data)
    {
        // Extract encrypted key, IV and data
        var encryptedKey = new byte[_rsa.KeySize / 8]; // Adjust according to key size
        var encryptedIV = new byte[_rsa.KeySize / 8]; // Same size as key for simplicity
        var encryptedData = new byte[data.Length - 2 * encryptedKey.Length];

        Buffer.BlockCopy(data, 0, encryptedKey, 0, encryptedKey.Length);
        Buffer.BlockCopy(data, encryptedKey.Length, encryptedIV, 0, encryptedIV.Length);
        Buffer.BlockCopy(data, encryptedKey.Length + encryptedIV.Length, encryptedData, 0,
            encryptedData.Length);

        // Decrypt key and IV
        var aesKey = _rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);
        var aesIV = _rsa.Decrypt(encryptedIV, RSAEncryptionPadding.OaepSHA256);

        // Decrypt data
        using var decryptor = _aesCng.CreateDecryptor(aesKey, aesIV);
        using var resultStream = _memoryStreamManager.GetStream("AesEncryptor-Decrypt");
        using var cryptoStream = new CryptoStream(resultStream, decryptor, CryptoStreamMode.Write);
        cryptoStream.Write(encryptedData, 0, encryptedData.Length);
        cryptoStream.FlushFinalBlock();

        // Correct handling of the read operation
        resultStream.Position = 0; // Reset the stream position to the beginning before reading
        var decryptedData = new byte[resultStream.Length];
        int bytesReadTotal = 0, bytesRead = 0;
        while ((bytesRead = resultStream.Read(decryptedData, bytesReadTotal, decryptedData.Length - bytesReadTotal)) >
               0) bytesReadTotal += bytesRead;

        return decryptedData.Take(bytesReadTotal).ToArray(); // Only return the actual data read
    }


    private void InitializeCryptoComponents()
    {
        using var rng = RandomNumberGenerator.Create();
        _aesCng.Key = new byte[32]; // 256 bits for AES-256
        _aesCng.IV = new byte[16]; // 128 bits for AES block size
        rng.GetBytes(_aesCng.Key);
        rng.GetBytes(_aesCng.IV);
    }

    private static byte[] Combine(params byte[][] arrays)
    {
        var totalLength = arrays.Sum(array => array.Length);
        var result = new byte[totalLength];
        var offset = 0;
        foreach (var array in arrays)
        {
            Buffer.BlockCopy(array, 0, result, offset, array.Length);
            offset += array.Length;
        }

        return result;
    }
}
