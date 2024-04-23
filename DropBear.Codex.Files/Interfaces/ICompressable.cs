using DropBear.Codex.Serialization.Interfaces;

namespace DropBear.Codex.Files.Interfaces;

public interface ICompressible : IEncryptable, IBuildable
{
    IEncryptable WithCompression<T>() where T : ICompressionProvider;
    IBuildable NoCompression();
}
