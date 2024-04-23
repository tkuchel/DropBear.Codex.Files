using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Interfaces;

public interface IBuildable
{
    Task<ContentContainer> BuildAsync();
}
