using System.Text;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Utilities.Hashing;
using MessagePack;
using ServiceStack.Text;

namespace DropBear.Codex.Files.Models.FileComponents;

/// <summary>
///     Represents the content of a file, managing a collection of content containers.
/// </summary>
[MessagePackObject]
public class FileContent : IFileContent
{
    [SerializationConstructor]
    public FileContent()
    {
    }

    /// <summary>
    ///     Internal storage for content containers, used for serialization and deserialization.
    ///     Direct access to this property is not intended outside of serialization concerns.
    /// </summary>
    [Key(0)]
    private List<IContentContainer> ContentsSerialization { get; } = new();

    /// <summary>
    ///     Gets a read-only view of the content containers in the file content.
    /// </summary>
    [IgnoreMember]
    public IReadOnlyList<IContentContainer> Contents => ContentsSerialization.AsReadOnly();

    /// <summary>
    ///     Adds a content container to the file content.
    /// </summary>
    /// <param name="content">The content container to add. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="content" /> is null.</exception>
    public void AddContent(IContentContainer content)
    {
        if (content is null) throw new ArgumentNullException(nameof(content), "Content cannot be null.");
        ContentsSerialization.Add(content);
    }

    /// <summary>
    ///     Removes a content container from the file content.
    /// </summary>
    /// <param name="content">The content container to remove. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="content" /> is null.</exception>
    public void RemoveContent(IContentContainer content)
    {
        if (content is null) throw new ArgumentNullException(nameof(content), "Content cannot be null.");
        ContentsSerialization.Remove(content);
    }

    /// <summary>
    ///     Clears all content containers from the file content.
    /// </summary>
    public void ClearContents() => ContentsSerialization.Clear();


    public IEnumerable<T> GetAllContents<T>() where T : class =>
        Contents
            .Where(c => c.ContentType.TypeName == typeof(T).Name)
            .Select(c => Deserialize<T>(c.GetData()))
            .Where(c => c is not null)!; // Assuming Deserialize<T> can return null, filter out those results.

    // Modified GetContent<T> with hash verification
    public T? GetContent<T>() where T : class
    {
        var container = FindContainerForType<T>();
        if (container is null) return null;

        var data = container.GetData();
        return !VerifyHash(container, data)
            ? null
            : // Hash verification failed
            Deserialize<T>(data);
    }

    // Modified GetRawContent with hash verification
    public byte[]? GetRawContent(string contentTypeName)
    {
        var container = Contents.FirstOrDefault(c => c.ContentType.TypeName == contentTypeName);
        if (container is null || !VerifyHash(container, container.GetData())) return null; // Hash verification failed

        return container.GetData();
    }

    private IContentContainer? FindContainerForType<T>() where T : class =>
        Contents.FirstOrDefault(c => c.ContentType.TypeName == typeof(T).Name);

    private static T? Deserialize<T>(byte[] data) where T : class
    {
        var json = Encoding.UTF8.GetString(data);
        // Adjusted to use ServiceStack.Text for consistency with initial description
        return JsonSerializer.DeserializeFromString<T>(json);
    }

    private static bool VerifyHash(IContentContainer container, byte[] data)
    {
        var hasher = new Blake3HashingService(); // Consider making this a shared instance or injected dependency
        var computedHash = hasher.EncodeToBase64Hash(data);

        return computedHash.IsSuccess && computedHash.Value == container.VerificationHash;
    }
}
