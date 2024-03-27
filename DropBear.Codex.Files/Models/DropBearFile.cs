using System.IO.Compression;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.Bases;
using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using static System.DateTimeOffset;

namespace DropBear.Codex.Files.Models;

public class DropBearFile(IFileMetaData metaData, ICompressionSettings compressionSettings, IFileContent content)
    : FileBase
{
    public IFileHeader Header { get; private set; } = new FileHeader();
    public IFileMetaData MetaData { get; private set; } = metaData;
    public ICompressionSettings CompressionSettings { get; private set; } = compressionSettings;
    public IFileContent Content { get; } = content;
}
