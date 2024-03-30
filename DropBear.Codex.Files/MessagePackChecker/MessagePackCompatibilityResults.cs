using System.Collections.ObjectModel;

namespace DropBear.Codex.Files.MessagePackChecker;

public class MessagePackCompatibilityResults
{
    public Collection<string> SuccessTypes { get; set; } = [];
    public Dictionary<string, string> FailedTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
