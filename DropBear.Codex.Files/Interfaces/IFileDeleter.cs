namespace DropBear.Codex.Files.Interfaces;

public interface IFileDeleter
{
    Task DeleteFileAsync(string filePath);
}
