using System.Collections.ObjectModel;

namespace DropBear.Codex.Files.MessagePackChecker;

/// <summary>
///     Represents the results of MessagePack compatibility checks.
/// </summary>
public class MessagePackCompatibilityResults
{
    /// <summary>
    ///     Gets or sets the collection of types that passed the compatibility check.
    /// </summary>
    public Collection<string> SuccessTypes { get; set; } = [];

    /// <summary>
    ///     Gets or sets the dictionary of types that failed the compatibility check, along with failure reasons.
    /// </summary>
    public Dictionary<string, string> FailedTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
