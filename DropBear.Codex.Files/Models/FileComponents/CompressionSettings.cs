using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.Bases;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents;

/// <summary>
///     Represents compression settings for file operations.
/// </summary>
[MessagePackObject]
public class CompressionSettings : FileComponentBase, ICompressionSettings
{
    [SerializationConstructor]
    public CompressionSettings()
    {
        
    }
    /// <summary>
    ///     Initializes a new instance of the <see cref="CompressionSettings" /> class with the specified compression settings.
    /// </summary>
    /// <param name="isCompressed">A value indicating whether compression is enabled.</param>
    public CompressionSettings(bool isCompressed) => IsCompressed = isCompressed;

    /// <summary>
    ///     Gets or sets a value indicating whether compression is enabled.
    /// </summary>
    [Key(0)]
    public bool IsCompressed { get; set; }
}
