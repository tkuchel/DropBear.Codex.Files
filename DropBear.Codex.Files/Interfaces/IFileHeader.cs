using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;

namespace DropBear.Codex.Files.Interfaces;

/// <summary>
///     Interface representing a file header.
/// </summary>
[MessagePack.Union(0, typeof(FileHeader))]
public interface IFileHeader
{
    /// <summary>
    ///     Gets the version of the file.
    /// </summary>
    FileHeaderVersion Version { get; }

    /// <summary>
    ///     Gets the signature of the file.
    /// </summary>
    FileSignature FileSignature { get; }
}
