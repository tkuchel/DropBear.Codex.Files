using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.Bases;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using DropBear.Codex.Utilities.Hashing;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents;

[MessagePackObject]
public class FileMetaData : FileComponentBase, IFileMetaData
{
    private readonly Blake3HashingService _hasher = new();

    [SerializationConstructor]
    public FileMetaData(string author, IFileContent content)
    {
        Author = author ?? throw new ArgumentNullException(nameof(author));
        ArgumentNullException.ThrowIfNull(content);

        // Populate the private collections
        ContentTypesSerialization.AddRange(content.Contents.Select(c => c.ContentType));
        UpdateVerificationHashes(content);
    }

    // Use private mutable collection for serialization
    [Key(4)] private List<ContentTypeInfo> ContentTypesSerialization { get; } = new();

    // Use private mutable dictionary for serialization
    [Key(0)] private Dictionary<ContentTypeInfo, string> VerificationHashesSerialization { get; } = new();

    public IReadOnlyDictionary<ContentTypeInfo, string> VerificationHashes => VerificationHashesSerialization;

    [Key(1)] public DateTimeOffset Created { get; } = DateTimeOffset.UtcNow;

    [Key(2)] public DateTimeOffset LastModified { get; private set; } = DateTimeOffset.UtcNow;

    [Key(3)] public string Author { get; }

    public IReadOnlyCollection<ContentTypeInfo> ExpectedContentTypes => ContentTypesSerialization.AsReadOnly();

    public void UpdateLastModified() => LastModified = DateTimeOffset.UtcNow;

    private void UpdateVerificationHashes(IFileContent content)
    {
        VerificationHashesSerialization.Clear();
        foreach (var contentContainer in content.Contents)
        {
            var hashResult = _hasher.EncodeToBase64Hash(contentContainer.GetData());
            if (hashResult.IsSuccess)
                VerificationHashesSerialization[contentContainer.ContentType] = hashResult.Value;
            // Consider handling failure case
        }
    }
}
