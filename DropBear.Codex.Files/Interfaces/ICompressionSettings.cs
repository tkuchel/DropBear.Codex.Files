using System.IO.Compression;

namespace DropBear.Codex.Files.Interfaces;

public interface ICompressionSettings
{
    public bool IsCompressed { get; set; }
    public CompressionLevel CompressionLevel { get; set; }
}
