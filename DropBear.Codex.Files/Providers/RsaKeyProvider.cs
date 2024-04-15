using System.Security.Cryptography;
using System.Text;
using DropBear.Codex.Files.Interfaces;
using static System.Security.Cryptography.ProtectedData;

namespace DropBear.Codex.Files.Providers;

public class RsaKeyProvider : IRsaKeyProvider
{
    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;

    public RsaKeyProvider(string publicKeyPath, string privateKeyPath)
    {
        _publicKeyPath = publicKeyPath;
        _privateKeyPath = privateKeyPath;
    }

    public RSA GetRsaProvider()
    {
        var rsa = RSA.Create();
        if (File.Exists(_privateKeyPath))
        {
            // Load existing keys
            rsa.ImportParameters(LoadKeyFromFile(_privateKeyPath, true));
        }
        else
        {
            // Generate and save new keys
            rsa.KeySize = 2048;
            var publicKey = rsa.ExportParameters(false);
            var privateKey = rsa.ExportParameters(true);
            SaveKeyToFile(_publicKeyPath, publicKey, false);
            SaveKeyToFile(_privateKeyPath, privateKey, true);
        }

        return rsa;
    }

    private static void SaveKeyToFile(string filePath, RSAParameters parameters, bool isPrivate)
    {
        var keyString = ConvertToXmlString(parameters, isPrivate);
        if (isPrivate)
        {
            // Encrypt the private key using DPAPI before saving it
            var encryptedKey =
                Protect(Encoding.UTF8.GetBytes(keyString), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(filePath, encryptedKey);
        }
        else
        {
            File.WriteAllText(filePath, keyString);
        }
    }

    private static RSAParameters LoadKeyFromFile(string filePath, bool isPrivate)
    {
        if (isPrivate)
        {
            var encryptedKey = File.ReadAllBytes(filePath);
            var decryptedKey = Unprotect(encryptedKey, null, DataProtectionScope.CurrentUser);
            var keyXml = Encoding.UTF8.GetString(decryptedKey);
            return ConvertFromXmlString(keyXml, isPrivate);
        }
        else
        {
            var keyXml = File.ReadAllText(filePath);
            return ConvertFromXmlString(keyXml, isPrivate);
        }
    }

    private static string ConvertToXmlString(RSAParameters parameters, bool includePrivateParameters)
    {
        using var rsa = RSA.Create();
        rsa.ImportParameters(parameters);
        return
            rsa.ToXmlString(
                includePrivateParameters); // Replace with actual serialization method if not using .NET Framework
    }

    private static RSAParameters ConvertFromXmlString(string xml, bool includePrivateParameters)
    {
        using var rsa = RSA.Create();
        rsa.FromXmlString(xml); // Replace with actual deserialization method if not using .NET Framework
        return rsa.ExportParameters(includePrivateParameters);
    }
}
