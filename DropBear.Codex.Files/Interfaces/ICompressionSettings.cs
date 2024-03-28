namespace DropBear.Codex.Files.Interfaces;

/// <summary>
///     Interface representing compression settings for file operations.
/// </summary>
public interface ICompressionSettings
{
    /// <summary>
    ///     Gets or sets a value indicating whether compression is enabled.
    /// </summary>
    bool IsCompressed { get; set; }
}
