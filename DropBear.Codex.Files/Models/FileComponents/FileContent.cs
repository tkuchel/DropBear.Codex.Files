using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.Bases;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents;

/// <summary>
///     Represents the content of a file.
/// </summary>
[MessagePackObject]
public class FileContent : FileComponentBase, IFileContent
{
    // Backing field for the Contents collection
    private readonly List<IContentContainer> _contents = new();

    /// <summary>
    ///     Gets the content containers in the file content.
    /// </summary>
    [Key(0)]
    public IReadOnlyList<IContentContainer> Contents => _contents.AsReadOnly();

    /// <summary>
    ///     Adds a content container to the file content.
    /// </summary>
    /// <param name="content">The content container to add.</param>
    public void AddContent(IContentContainer content)
    {
        if (content is null) throw new ArgumentNullException(nameof(content), "Content cannot be null.");
        _contents.Add(content);
    }

    /// <summary>
    ///     Removes a content container from the file content.
    /// </summary>
    /// <param name="content">The content container to remove.</param>
    public void RemoveContent(IContentContainer content)
    {
        if (content is null) throw new ArgumentNullException(nameof(content), "Content cannot be null.");
        _contents.Remove(content);
    }

    /// <summary>
    ///     Clears all content containers from the file content.
    /// </summary>
    public void ClearContents() => _contents.Clear();
}
