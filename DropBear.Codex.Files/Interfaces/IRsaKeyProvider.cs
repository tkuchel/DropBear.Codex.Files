using System.Security.Cryptography;

namespace DropBear.Codex.Files.Interfaces;

public interface IRsaKeyProvider
{
    RSA GetRsaProvider();
}
