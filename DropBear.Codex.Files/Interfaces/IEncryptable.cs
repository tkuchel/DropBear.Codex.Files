using DropBear.Codex.Serialization.Interfaces;

namespace DropBear.Codex.Files.Interfaces;

public interface IEncryptable : IBuildable
{
    IBuildable WithEncryption<T>() where T : IEncryptionProvider;
    IBuildable NoEncryption();
}
