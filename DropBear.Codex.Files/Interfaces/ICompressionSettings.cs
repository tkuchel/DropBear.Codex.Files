using DropBear.Codex.Files.Models.FileComponents;

namespace DropBear.Codex.Files.Interfaces;

/// <summary>
///     Interface representing compression settings for file operations.
/// </summary>
[MessagePack.Union(0, typeof(CompressionSettings))]
public interface ICompressionSettings
{
    /// <summary>
    ///     Gets or sets a value indicating whether compression is enabled.
    /// </summary>
    bool IsCompressed { get; set; }
}
