using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileManager
{
    /// <summary>
    ///     Creates a DropBear file asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of content to include in the file.</typeparam>
    /// <param name="name">The name of the file.</param>
    /// <param name="content">The content to include in the file.</param>
    /// <param name="compress">Whether to compress the file content.</param>
    /// <param name="contentType">The type of content (optional).</param>
    /// <param name="forceCreation">Whether to force the creation of the file even if validation fails.</param>
    /// <returns>The created DropBear file.</returns>
    Task<Result<DropBearFile>> CreateFileAsync<T>(string name, T content, bool compress = false,
        Type? contentType = null, bool forceCreation = false) where T : class;

    /// <summary>
    ///     Writes a DropBear file asynchronously.
    /// </summary>
    /// <param name="file">The DropBear file to write.</param>
    /// <param name="filePath">The path where the file should be written.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteFileAsync(DropBearFile file, string filePath);

    /// <summary>
    ///     Reads a DropBear file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the DropBear file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The read DropBear file.</returns>
    Task<Result<DropBearFile>> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    Task DeleteFileAsync(string filePath);

    /// <summary>
    ///     Updates a file with new content.
    /// </summary>
    /// <param name="filePath">The path to the file to update.</param>
    /// <param name="newContent">The new content for the file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateFileAsync(string filePath, DropBearFile newContent, CancellationToken cancellationToken = default);
}
