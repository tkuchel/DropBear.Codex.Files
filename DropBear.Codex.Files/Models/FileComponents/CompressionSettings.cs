using System.IO.Compression;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.Bases;

namespace DropBear.Codex.Files.Models.FileComponents;

public class CompressionSettings : FileComponentBase, ICompressionSettings
{
    public bool IsCompressed { get; set; }
    public CompressionLevel CompressionLevel { get; set; }
}
