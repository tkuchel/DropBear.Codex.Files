using DropBear.Codex.Files.Services;

namespace DropBear.Codex.Files.Interfaces;

public interface IBuildStep
{
    FileManager Build();
}
