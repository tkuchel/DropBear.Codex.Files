using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileCreator
{
    IFileCreator WithCompression();

    Task<Result<DropBearFile>> CreateAsync<T>(string name, T content, bool forceCreation = false) where T : class;
}