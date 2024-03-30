using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;

namespace DropBear.Codex.Files.Interfaces;

/// <summary>
///     Interface representing a file header.
/// </summary>
[Union(0, typeof(FileHeader))]
public interface IFileHeader
{
    /// <summary>
    ///     Gets the version of the file.
    /// </summary>
    [Key(0)]
    FileHeaderVersion Version { get; }

    /// <summary>
    ///     Gets the signature of the file.
    /// </summary>
    [Key(1)]
    FileSignature FileSignature { get; }
}
