using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.Bases;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using DropBear.Codex.Utilities.Hashing;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents;

/// <summary>
///     Represents metadata information for a file, including content types, verification hashes, and author details.
/// </summary>
[MessagePackObject]
public class FileMetaData : FileComponentBase, IFileMetaData
{
    private readonly List<ContentTypeInfo> _contentTypes = new();
    private readonly Blake3HashingService _hasher = new();
    private readonly Dictionary<ContentTypeInfo, string> _verificationHashes = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="FileMetaData" /> class with the specified author and file content.
    /// </summary>
    /// <param name="author">The author of the file.</param>
    /// <param name="content">The content of the file.</param>
    [SerializationConstructor]
    public FileMetaData(string author, IFileContent content)
    {
        Author = author ?? throw new ArgumentNullException(nameof(author));
        ArgumentNullException.ThrowIfNull(content);

        // Initialize content types from the provided content
        _contentTypes.AddRange(content.Contents.Select(contentContainer => contentContainer.ContentType));

        UpdateVerificationHashes(content);
    }

    /// <summary>
    ///     Gets a read-only dictionary of content types to their corresponding verification hashes.
    /// </summary>
    [Key(0)]
    public IReadOnlyDictionary<ContentTypeInfo, string> VerificationHashes => _verificationHashes;

    /// <summary>
    ///     Gets the creation date and time of the file.
    /// </summary>
    [Key(1)]
    public DateTimeOffset Created { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Gets or sets the last modified date and time of the file.
    /// </summary>
    [Key(2)]
    public DateTimeOffset LastModified { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Gets the author of the file.
    /// </summary>
    [Key(3)]
    public string Author { get; }

    /// <summary>
    ///     Gets a read-only collection of expected content types for the file.
    /// </summary>
    [Key(4)]
    public IReadOnlyCollection<ContentTypeInfo> ExpectedContentTypes => _contentTypes.AsReadOnly();

    /// <summary>
    ///     Updates the last modified timestamp to the current time.
    /// </summary>
    public void UpdateLastModified() => LastModified = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Updates the verification hashes for each content type based on the current file content.
    /// </summary>
    /// <param name="content">The content of the file.</param>
    private void UpdateVerificationHashes(IFileContent content)
    {
        // Ensure existing verification hashes are cleared to avoid stale data
        _verificationHashes.Clear();

        foreach (var contentContainer in content.Contents)
        {
            var hashResult = _hasher.EncodeToBase64Hash(contentContainer.Data);

            if (hashResult.IsSuccess) _verificationHashes[contentContainer.ContentType] = hashResult.Value;
            // Consider handling failure case, possibly logging or throwing an exception
        }
    }
}
