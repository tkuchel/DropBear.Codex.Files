using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.Bases;
using DropBear.Codex.Files.Models.FileComponents;
using MessagePack;

namespace DropBear.Codex.Files.Models;

/// <summary>
///     Represents a DropBear file with metadata, compression settings, and content.
/// </summary>
[MessagePackObject]
public class DropBearFile : FileBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DropBearFile" /> class with the specified metadata, compression
    ///     settings, and content.
    /// </summary>
    /// <param name="metaData">The metadata of the file.</param>
    /// <param name="compressionSettings">The compression settings of the file.</param>
    /// <param name="content">The content of the file.</param>
    public DropBearFile(IFileMetaData metaData, ICompressionSettings compressionSettings, IFileContent content)
    {
        MetaData = metaData;
        CompressionSettings = compressionSettings;
        Content = content;
    }

    /// <summary>
    ///     Gets or sets the header of the DropBear file.
    /// </summary>
    [Key(0)]
    public IFileHeader Header { get; private set; } = new FileHeader();

    /// <summary>
    ///     Gets or sets the metadata of the DropBear file.
    /// </summary>
    [Key(1)]
    public IFileMetaData MetaData { get; private set; }

    /// <summary>
    ///     Gets or sets the compression settings of the DropBear file.
    /// </summary>
    [Key(2)]
    public ICompressionSettings CompressionSettings { get; private set; }

    /// <summary>
    ///     Gets the content of the DropBear file.
    /// </summary>
    [Key(3)]
    public IFileContent Content { get; }

    /// <summary>
    ///     Reconstructs a DropBear file with the specified header, metadata, compression settings, and content.
    /// </summary>
    /// <param name="header">The header of the file.</param>
    /// <param name="fileMetaData">The metadata of the file.</param>
    /// <param name="compressionSettings">The compression settings of the file.</param>
    /// <param name="fileContent">The content of the file.</param>
    /// <returns>The reconstructed DropBear file.</returns>
    public static DropBearFile Reconstruct(FileHeader header, FileMetaData fileMetaData,
        CompressionSettings compressionSettings, IFileContent fileContent) =>
        new(fileMetaData, compressionSettings, fileContent) { Header = header };
}
