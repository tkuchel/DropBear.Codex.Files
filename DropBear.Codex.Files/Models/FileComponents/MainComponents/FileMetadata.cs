using System.Collections.ObjectModel;
using Blake3;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.MainComponents;

[MessagePackObject]
public class FileMetadata
{
    [Key(0)] public string FileName { get; set; } = string.Empty;

    [Key(1)] public int FileSize { get; private set; }

    [Key(2)] public DateTimeOffset FileCreatedDate { get; internal set; } = DateTimeOffset.UtcNow;

    [Key(3)] public DateTimeOffset FileModifiedDate { get; internal set; } = DateTimeOffset.UtcNow;

    [Key(4)] public string FileOwner { get; set; } = string.Empty;

    [Key(5)] public Collection<ContentTypeInfo> ContentTypes { get; } = [];

    [Key(6)]
    public Dictionary<string, string> ContentTypeVerificationHashes { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    [Key(7)]
    public Dictionary<string, string> CustomMetadata { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public void UpdateWithNewContent(IContentContainer content)
    {
        UpdateModifiedDate();
        FileSize += content.Length;
        AddContentTypeAndHash(content.ContentType, content.Content());
    }

    private void AddContentTypeAndHash(ContentTypeInfo contentType, byte[] content)
    {
        var hash = Hasher.Hash(content).ToString();
        ContentTypeVerificationHashes[contentType.TypeName] = hash;
        if (ContentTypes.All(ct => ct.TypeName != contentType.TypeName)) ContentTypes.Add(contentType);
    }

    /// <summary>
    ///     Updates the last modified date to the current time.
    /// </summary>
    public void UpdateModifiedDate() => FileModifiedDate = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Updates the file size to the provided value.
    /// </summary>
    /// <param name="size">The new size of the file.</param>
    public void UpdateFileSize(int size) => FileSize = size;


    /// <summary>
    ///     Adds or updates custom metadata key-value pairs.
    /// </summary>
    /// <param name="key">The key of the metadata.</param>
    /// <param name="value">The value of the metadata.</param>
    public void AddOrUpdateCustomMetadata(string key, string value)
    {
        CustomMetadata[key] = value;
        UpdateModifiedDate();
    }

    /// <summary>
    ///     Removes a custom metadata entry by key.
    /// </summary>
    /// <param name="key">The key of the metadata to remove.</param>
    /// <returns>True if the metadata was successfully removed, otherwise false.</returns>
    public bool RemoveCustomMetadata(string key)
    {
        UpdateModifiedDate();
        return CustomMetadata.Remove(key);
    }

    /// <summary>
    ///     Retrieves a custom metadata value by key.
    /// </summary>
    /// <param name="key">The key of the metadata to retrieve.</param>
    /// <returns>The value of the metadata, or an empty string if not found.</returns>
    public string GetCustomMetadata(string key)
    {
        CustomMetadata.TryGetValue(key, out var value);
        return value ?? string.Empty;
    }

    /// <summary>
    ///     Removes a content type from the list by its type name.
    /// </summary>
    /// <param name="typeName">The type name of the content type to remove.</param>
    /// <returns>True if the content type was successfully removed, otherwise false.</returns>
    public bool RemoveContentType(string typeName)
    {
        UpdateModifiedDate();
        var contentType = ContentTypes.FirstOrDefault(ct => ct.TypeName == typeName);
        return contentType is not null && ContentTypes.Remove(contentType);
    }
}
