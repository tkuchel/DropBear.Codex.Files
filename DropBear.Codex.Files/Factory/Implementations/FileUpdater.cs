using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileUpdater : IFileUpdater
{
    public async Task<Result<DropBearFile>> UpdateFileAsync(string filePath, DropBearFile newContent, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
