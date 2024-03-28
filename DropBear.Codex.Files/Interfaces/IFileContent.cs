namespace DropBear.Codex.Files.Interfaces;

/// <summary>
///     Interface representing file content.
/// </summary>
public interface IFileContent
{
    /// <summary>
    ///     Gets the contents of the file.
    /// </summary>
    IReadOnlyList<IContentContainer> Contents { get; }

    /// <summary>
    ///     Adds content to the file.
    /// </summary>
    /// <param name="content">The content to add.</param>
    void AddContent(IContentContainer content);

    /// <summary>
    ///     Removes content from the file.
    /// </summary>
    /// <param name="content">The content to remove.</param>
    void RemoveContent(IContentContainer content);

    /// <summary>
    ///     Clears all contents from the file.
    /// </summary>
    void ClearContents();
}
