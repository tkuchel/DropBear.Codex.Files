using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;

namespace DropBear.Codex.Files.Interfaces;

/// <summary>
///     Represents the contract for managing and accessing file metadata,
///     including content types, verification hashes, and author details.
/// </summary>
[Union(0, typeof(FileMetaData))]
public interface IFileMetaData
{
    /// <summary>
    ///     Gets a read-only dictionary mapping content types to their corresponding verification hashes.
    /// </summary>
    IReadOnlyDictionary<ContentTypeInfo, string> VerificationHashes { get; }

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
    ///     Gets a read-only collection of content types expected for the file.
    /// </summary>
    IReadOnlyCollection<ContentTypeInfo> ExpectedContentTypes { get; }

    /// <summary>
    ///     Updates the last modified timestamp to the current time.
    /// </summary>
    void UpdateLastModified();
}
