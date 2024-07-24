#region

using DropBear.Codex.Files.Services;

#endregion

namespace DropBear.Codex.Files.Interfaces;

public interface IBuildStep
{
    FileManager Build();
}
