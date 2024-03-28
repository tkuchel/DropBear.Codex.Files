using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Interfaces;

/// <summary>
///     Interface representing a file header.
/// </summary>
public interface IFileHeader
{
    /// <summary>
    ///     Gets the version of the file.
    /// </summary>
    Version Version { get; }

    /// <summary>
    ///     Gets the signature of the file.
    /// </summary>
    FileSignature FileSignature { get; }
}
