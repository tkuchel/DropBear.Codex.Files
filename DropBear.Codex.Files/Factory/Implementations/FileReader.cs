using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileReader : IFileReader
{
    public async Task<Result<DropBearFile>> ReadFileAsync(string filePath, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
