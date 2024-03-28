using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

/// <summary>
///     Represents information about a content type.
/// </summary>
[MessagePackObject]
public class ContentTypeInfo
{
    /// <summary>
    ///     Gets or sets the name of the assembly.
    /// </summary>
    [Key(0)]
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the name of the type.
    /// </summary>
    [Key(1)]
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the namespace of the type.
    /// </summary>
    [Key(2)]
    public string Namespace { get; set; } = string.Empty;
}
