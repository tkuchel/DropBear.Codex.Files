#region

using DropBear.Codex.Serialization.Interfaces;

#endregion

namespace DropBear.Codex.Files.Interfaces;

public interface IEncryptable : IBuildable
{
    IBuildable WithEncryption<T>() where T : IEncryptionProvider;
    IBuildable NoEncryption();
}
