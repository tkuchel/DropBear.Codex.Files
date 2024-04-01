using Blake3;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using DropBear.Codex.Utilities.Helpers;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.MainComponents;

[MessagePackObject]
public class FileMetadata
{
    [Key(0)] public string FileName { get; set; } = string.Empty;
    [Key(1)] public int FileSize { get; set; }

    [Key(2)] public DateTimeOffset FileCreatedDate { get; set; } = DateTimeOffset.UtcNow;

    [Key(3)] public DateTimeOffset FileModifiedDate { get; set; } = DateTimeOffset.UtcNow;

    [Key(4)] public string FileOwner { get; set; } = string.Empty;

    [Key(5)] public List<ContentTypeInfo> ContentTypes { get; set; } = [];

    [Key(6)]
    public Dictionary<string, string> ContentTypeVerificationHashes { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    [Key(7)] public Dictionary<string, string> CustomMetadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Updates the FileModifiedDate to the current time
    public void UpdateModifiedDate() => FileModifiedDate = DateTimeOffset.UtcNow;

    // Updates the FileSize to the provided value
    public void UpdateFileSize(int size) => FileSize = size;

    // Adds a new content type and hash to the ContentTypeVerificationHashes
    public void AddContentTypeAndHash(ContentTypeInfo contentType, byte[] content)
    {
        var hash = Hasher.Hash(content).ToString();
        ContentTypeVerificationHashes[contentType.TypeName] = hash;
        AddContentType(contentType);
    }

    // Adds or updates custom metadata key-value pairs
    public void AddOrUpdateCustomMetadata(string key, string value)
    {
        CustomMetadata[key] = value;
        UpdateModifiedDate();
    }

    // Removes a custom metadata entry by key
    public bool RemoveCustomMetadata(string key)
    {
        UpdateModifiedDate();
        return CustomMetadata.Remove(key);
    }

    // Retrieves a custom metadata value by key
    public string GetCustomMetadata(string key)
    {
        CustomMetadata.TryGetValue(key, out var value);
        return value ?? string.Empty;
    }

    private void AddContentType(ContentTypeInfo contentType)
    {
        if (ContentTypes.TrueForAll(ct => ct.TypeName != contentType.TypeName)) ContentTypes.Add(contentType);
        UpdateModifiedDate();
    }

    public bool RemoveContentType(string typeName)
    {
        UpdateModifiedDate();
        var contentType = ContentTypes.Find(ct => ct.TypeName == typeName);

        return contentType is not null && ContentTypes.Remove(contentType);
    }
}
