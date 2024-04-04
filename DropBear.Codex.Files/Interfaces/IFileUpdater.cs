using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileUpdater
{
    Task<Result<DropBearFile>> UpdateFileAsync(string filePath, DropBearFile newContent,
        CancellationToken cancellationToken = default);
}
