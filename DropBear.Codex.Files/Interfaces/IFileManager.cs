using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileManager
{
    Task<DropBearFile?> CreateFileAsync<T>(string name, T content, bool compress = false,
        Type? contentType = null) where T : class;

    Task WriteFileAsync(DropBearFile file, string filePath);
    Task<DropBearFile?> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);
    void DeleteFile(string filePath);

    Task UpdateFile(string filePath, DropBearFile newContent,
        CancellationToken cancellationToken = default);
}
