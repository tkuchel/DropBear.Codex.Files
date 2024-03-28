using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileManager
{
    /// <summary>
    ///     Creates a DropBear file asynchronously with the specified author, content, and optional compression settings.
    /// </summary>
    /// <param name="author">The author of the file.</param>
    /// <param name="content">The content of the file.</param>
    /// <param name="compressContent">Indicates whether the file should be compressed.</param>
    /// <returns>A task representing the asynchronous operation and containing the created DropBear file.</returns>
    Task<Result<DropBearFile>> CreateFileAsync(string author, IFileContent content,
        bool compressContent = false);

    /// <summary>
    ///     Writes a DropBear file to the specified file path asynchronously.
    /// </summary>
    /// <param name="file">The DropBear file to write.</param>
    /// <param name="filePath">The file path where the file will be written.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> WriteFileAsync(DropBearFile file, string filePath);

    /// <summary>
    ///     Reads a DropBear file asynchronously from the specified file path.
    /// </summary>
    /// <param name="filePath">The file path from which to read the file.</param>
    /// <returns>A task representing the asynchronous operation and containing the read DropBear file.</returns>
    Task<Result<DropBearFile>> ReadFileAsync(string filePath);

    /// <summary>
    ///     Deletes the file at the specified file path.
    /// </summary>
    /// <param name="filePath">The file path of the file to delete.</param>
    /// <returns>A result indicating the outcome of the operation.</returns>
    Result DeleteFile(string filePath);

    /// <summary>
    ///     Updates the specified DropBear file asynchronously at the specified file path.
    /// </summary>
    /// <param name="file">The DropBear file to update.</param>
    /// <param name="filePath">The file path where the updated file will be written.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result<DropBearFile>> UpdateFileAsync(DropBearFile file, string filePath);
}
