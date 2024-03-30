using DropBear.Codex.Files.Models.FileComponents;

namespace DropBear.Codex.Files.Interfaces;

/// <summary>
///     Defines the functionality for managing the content of a file,
///     including adding, removing, and clearing content containers.
/// </summary>
[MessagePack.Union(0, typeof(FileContent))]
public interface IFileContent
{
    /// <summary>
    ///     Gets an immutable list of content containers within the file.
    /// </summary>
    IReadOnlyList<IContentContainer> Contents { get; }

    /// <summary>
    ///     Adds a content container to the file.
    /// </summary>
    /// <param name="content">The content container to add. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content" /> is null.</exception>
    void AddContent(IContentContainer content);

    /// <summary>
    ///     Removes a specific content container from the file.
    /// </summary>
    /// <param name="content">The content container to remove. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content" /> is null.</exception>
    void RemoveContent(IContentContainer content);

    /// <summary>
    ///     Clears all content containers from the file, resulting in an empty content list.
    /// </summary>
    void ClearContents();
}
