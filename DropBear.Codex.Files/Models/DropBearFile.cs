using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents;
using MessagePack;

namespace DropBear.Codex.Files.Models;

/// <summary>
///     Represents a DropBear file with metadata, compression settings, and content.
/// </summary>
[MessagePackObject]
public class DropBearFile
{
    [SerializationConstructor]
    public DropBearFile(IFileMetaData metaData, ICompressionSettings compressionSettings, IFileContent content)
    {
        MetaData = metaData;
        CompressionSettings = compressionSettings;
        Content = content;
    }

    [Key(0)] public IFileHeader Header { get; private set; } = new FileHeader();

    [Key(1)] public IFileMetaData MetaData { get; private set; }

    [Key(2)] public ICompressionSettings CompressionSettings { get; private set; }

    [Key(3)] public IFileContent Content { get; }

    public static DropBearFile Reconstruct(FileHeader header, FileMetaData fileMetaData,
        CompressionSettings compressionSettings, IFileContent fileContent) =>
        new(fileMetaData, compressionSettings, fileContent) { Header = header };

    /// <summary>
    ///     Retrieves a specific type of content from the DropBear file, if present.
    /// </summary>
    /// <typeparam name="T">The type of content to retrieve.</typeparam>
    /// <returns>An instance of the requested content type if found; otherwise, null.</returns>
    public T? GetContent<T>() where T : class => Content.GetContent<T>();

    /// <summary>
    ///     Retrieves the raw byte data for a specific type of content, identified by its content type name.
    /// </summary>
    /// <param name="contentTypeName">The name of the content type for which to retrieve the raw data.</param>
    /// <returns>The raw byte data if found; otherwise, null.</returns>
    public byte[]? GetRawContent(string contentTypeName) => Content.GetRawContent(contentTypeName);
}
