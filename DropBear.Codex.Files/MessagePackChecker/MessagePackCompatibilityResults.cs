using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace DropBear.Codex.Files.MessagePackChecker
{
    /// <summary>
    /// Represents the results of MessagePack compatibility checks.
    /// </summary>
    public class MessagePackCompatibilityResults
    {
        /// <summary>
        /// Gets or sets the collection of types that passed the compatibility check.
        /// </summary>
        public Collection<string> SuccessTypes { get; set; } = new Collection<string>();

        /// <summary>
        /// Gets or sets the dictionary of types that failed the compatibility check, along with failure reasons.
        /// </summary>
        public Dictionary<string, string> FailedTypes { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
