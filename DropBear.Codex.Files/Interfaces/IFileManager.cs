using System;
using System.Threading;
using System.Threading.Tasks;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Interfaces
{
    /// <summary>
    /// Interface for managing files.
    /// </summary>
    public interface IFileManager
    {
        /// <summary>
        /// Creates a new file asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of content to be stored in the file.</typeparam>
        /// <param name="name">The name of the file.</param>
        /// <param name="content">The content of the file.</param>
        /// <param name="compress">Specifies whether to compress the content.</param>
        /// <param name="contentType">The type of content.</param>
        /// <param name="forceCreation">Specifies whether to forcefully create the file.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the created file.</returns>
        Task<DropBearFile?> CreateFileAsync<T>(string name, T content, bool compress = false,
            Type? contentType = null, bool forceCreation = false) where T : class;

        /// <summary>
        /// Writes a file asynchronously.
        /// </summary>
        /// <param name="file">The file to write.</param>
        /// <param name="filePath">The path to write the file to.</param>
        Task WriteFileAsync(DropBearFile file, string filePath);

        /// <summary>
        /// Reads a file asynchronously.
        /// </summary>
        /// <param name="filePath">The path of the file to read.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the read file.</returns>
        Task<DropBearFile?> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="filePath">The path of the file to delete.</param>
        void DeleteFile(string filePath);

        /// <summary>
        /// Updates a file asynchronously.
        /// </summary>
        /// <param name="filePath">The path of the file to update.</param>
        /// <param name="newContent">The new content of the file.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        Task UpdateFile(string filePath, DropBearFile newContent, CancellationToken cancellationToken = default);
    }
}
