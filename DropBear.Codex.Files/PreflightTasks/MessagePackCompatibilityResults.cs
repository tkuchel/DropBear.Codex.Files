using System.Collections.ObjectModel;

namespace DropBear.Codex.Files.PreflightTasks;

public class MessagePackCompatibilityResults
{
    public Collection<string> SuccessTypes { get; set; } = new(Array.Empty<string>());
    public Dictionary<string, string> FailedTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
