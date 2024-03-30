using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using DropBear.Codex.Utilities.Hashing;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents;

[MessagePackObject]
public class FileMetaData : IFileMetaData
{
    private readonly Blake3HashingService _hasher = new();

    // Default constructor for MessagePack deserialization
    [SerializationConstructor]
    public FileMetaData()
    {
    }

    // Use this constructor for manual object creation
    public FileMetaData(string author, IFileContent content) : this()
    {
        Author = author ?? throw new ArgumentNullException(nameof(author));
        ArgumentNullException.ThrowIfNull(content);
        InitializeFromContent(content);
    }

    [Key(3)] public string Author { get; private set; }

    [Key(4)] private List<ContentTypeInfo> ContentTypesSerialization { get; set; } = new();

    [Key(0)] private Dictionary<ContentTypeInfo, string> VerificationHashesSerialization { get; set; } = new();

    [IgnoreMember]
    public IReadOnlyDictionary<ContentTypeInfo, string> VerificationHashes => VerificationHashesSerialization;

    [Key(1)] public DateTimeOffset Created { get; private set; } = DateTimeOffset.UtcNow;

    [Key(2)] public DateTimeOffset LastModified { get; private set; } = DateTimeOffset.UtcNow;

    [IgnoreMember]
    public IReadOnlyCollection<ContentTypeInfo> ExpectedContentTypes => ContentTypesSerialization.AsReadOnly();

    public void UpdateLastModified() => LastModified = DateTimeOffset.UtcNow;

    private void InitializeFromContent(IFileContent content)
    {
        // Populate the private collections from the content parameter
        ContentTypesSerialization.AddRange(content.Contents.Select(c => c.ContentType));
        UpdateVerificationHashes(content);
    }
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
