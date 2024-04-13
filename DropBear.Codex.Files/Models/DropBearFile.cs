using System.Collections.ObjectModel;
using System.Text;
using DropBear.Codex.Files.Exceptions;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents.MainComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using DropBear.Codex.Files.Utils;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DropBear.Codex.Files.Models;

/// <summary>
///     Represents a DropBear file, encapsulating header, metadata, and content with optional compression.
/// </summary>
[MessagePackObject]
public class DropBearFile
{
    /// <summary>
    ///     Initializes a new instance of the DropBearFile class with default properties.
    /// </summary>
    [Obsolete("For MessagePack", false)]
    public DropBearFile() : this(string.Empty, string.Empty) { }

    /// <summary>
    ///     Initializes a new instance of the DropBearFile class with specified properties.
    /// </summary>
    public DropBearFile(string fileName, string fileOwner, bool compressContent = false)
    {
        Header = new FileHeader(new FileVersion(), new FileSignature());
        Metadata = new FileMetadata
        {
            FileName = fileName,
            FileOwner = fileOwner,
            FileCreatedDate = DateTimeOffset.UtcNow,
            FileModifiedDate = DateTimeOffset.UtcNow
        };
        Content = new FileContent();
        CompressContent = compressContent;
    }

    [Key(0)] public FileHeader? Header { get; private init; }

    [Key(1)] public FileMetadata Metadata { get; private init; }

    [Key(2)] public FileContent Content { get; private init; }

    [Key(3)] public bool CompressContent { get; }

    private string GetFileName() => Metadata.FileName;

    private string GetExtension() => Header?.Signature.Extension ?? "dbf";

    public string GetFileNameWithExtension() => $"{GetFileName()}.{GetExtension()}";

    /// <summary>
    ///     Adds content to the file, automatically handling compression based on file settings.
    /// </summary>
    public void AddContent(IContentContainer content)
    {
        Content.AddContent(content);
        Metadata.UpdateWithNewContent(content);
    }

    public void AddContent(byte[] contentData, ContentTypeInfo contentType, string contentName, bool compress = false)
    {
        var shouldCompress = compress || CompressContent;
        var contentContainer = new ContentContainer(contentName, contentData, contentType, shouldCompress);
        AddContent(contentContainer);
    }

    /// <summary>
    ///     Verifies the integrity of the DropBear file, ensuring metadata and content hashes are consistent.
    /// </summary>
    public bool VerifyDropBearFileIntegrity() => VerifyMetadata() && VerifyContent();

    private bool VerifyMetadata()
    {
        // Verify file size (It turns out that the stored file size is only the Content size not metadata or header)
        var contentAsString = Content.Contents.Select(content => BitConverter.ToString(content.Content).Replace("-",string.Empty, StringComparison.OrdinalIgnoreCase)).Aggregate((current, next) => current + next);
        var totalContentSize = Content.Contents.Sum(content => content.Length);
        return Metadata.FileSize == totalContentSize;
    }

    private bool VerifyContent()
    {
        foreach (var content in Content.Contents)
        {
            if (content is not ContentContainer container)
                return false;
            if (container.VerifyContentHash())
                continue;
            return false;
        }

        return true;
    }

    public static DropBearFile Reconstruct(FileHeader header, FileMetadata metadata, FileContent content)
    {
        var dropBearFile = new DropBearFile(metadata.FileName, metadata.FileOwner)
        {
            Header = header, Metadata = metadata, Content = content
        };

        if (!dropBearFile.VerifyDropBearFileIntegrity())
            throw new IntegrityVerificationFailedException("Integrity verification failed.");
        return dropBearFile;
    }

    public static DropBearFile Reconstruct(bool useJsonSerialization, Collection<byte[]> components,
        CancellationToken cancellationToken = default)
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Converters = new List<JsonConverter> { new ContentContainerConverter(), new FileMetadataConverter() }
        };

        if (components.Count < 3)
            throw new ArgumentException("Insufficient components for reconstruction.", nameof(components));

        var header = useJsonSerialization
            ? JsonConvert.DeserializeObject<FileHeader>(Encoding.UTF8.GetString(components[0]), settings)
            : MessagePackSerializer.Deserialize<FileHeader>(components[0], cancellationToken: cancellationToken);
        var metadata = useJsonSerialization
            ? JsonConvert.DeserializeObject<FileMetadata>(Encoding.UTF8.GetString(components[1]), settings)
            : MessagePackSerializer.Deserialize<FileMetadata>(components[1], cancellationToken: cancellationToken);
        var content = useJsonSerialization
            ? JsonConvert.DeserializeObject<FileContent>(Encoding.UTF8.GetString(components[2]), settings)
            : MessagePackSerializer.Deserialize<FileContent>(components[2], cancellationToken: cancellationToken);

        if (header is null || metadata is null || content is null)
            throw new InvalidOperationException("One or more components are null.");

        return Reconstruct(header, metadata, content);
    }

}
