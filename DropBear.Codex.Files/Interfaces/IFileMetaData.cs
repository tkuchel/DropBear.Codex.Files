using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Interfaces;

/// <summary>
///     Interface representing file metadata.
/// </summary>
public interface IFileMetaData
{
    /// <summary>
    ///     Gets the creation date and time of the file.
    /// </summary>
    DateTimeOffset Created { get; }

    /// <summary>
    ///     Gets the last modified date and time of the file.
    /// </summary>
    DateTimeOffset LastModified { get; }

    /// <summary>
    ///     Gets the author of the file.
    /// </summary>
    string Author { get; }

    /// <summary>
    ///     Gets the expected content types of the file.
    /// </summary>
    IReadOnlyCollection<ContentTypeInfo> ExpectedContentTypes { get; }

    /// <summary>
    ///     Updates the last modified date and time of the file.
    /// </summary>
    void UpdateLastModified();
}
