#region

using DropBear.Codex.Serialization.Interfaces;

#endregion

namespace DropBear.Codex.Files.Interfaces;

public interface ICompressible : IEncryptable, IBuildable
{
    IEncryptable WithCompression<T>() where T : ICompressionProvider;
    IBuildable NoCompression();
}
