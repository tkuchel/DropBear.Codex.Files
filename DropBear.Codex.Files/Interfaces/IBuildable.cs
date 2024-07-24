#region

using DropBear.Codex.Files.Models;

#endregion

namespace DropBear.Codex.Files.Interfaces;

public interface IBuildable
{
    Task<ContentContainer> BuildAsync();
}
