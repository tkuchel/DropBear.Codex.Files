using DropBear.Codex.Core.ReturnTypes;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileDeleter
{
    Task<Result> DeleteFileAsync(string filePath);
}
