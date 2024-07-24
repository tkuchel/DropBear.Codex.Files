#region

using DropBear.Codex.Serialization.Interfaces;

#endregion

namespace DropBear.Codex.Files.Interfaces;

public interface IDataConfigurable
{
    ISerializable WithSerializer<T>() where T : ISerializer;
    ICompressible WithCompression<T>() where T : ICompressionProvider;
    IEncryptable WithEncryption<T>() where T : IEncryptionProvider;
    IBuildable NoSerialization();
}
