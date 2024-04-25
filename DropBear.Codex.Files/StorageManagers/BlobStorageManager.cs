using DropBear.Codex.Files.Interfaces;
using FluentStorage.Blobs;
using Microsoft.IO;

namespace DropBear.Codex.Files.StorageManagers;

public class BlobStorageManager
{
    private readonly IBlobStorage _blobStorage;
    private readonly string _defaultContainerName;
    private readonly RecyclableMemoryStreamManager _memoryStreamManager;

    public BlobStorageManager(IBlobStorage blobStorage, RecyclableMemoryStreamManager memoryStreamManager,
        string defaultContainerName = "default-container")
    {
        _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
        _memoryStreamManager = memoryStreamManager ?? throw new ArgumentNullException(nameof(memoryStreamManager));
        _defaultContainerName = defaultContainerName;
    }

    public async Task WriteAsync(string blobName, Stream dataStream, string containerName)
    {
        // Ensure the container name defaults if not provided
        containerName = string.IsNullOrEmpty(containerName) ? _defaultContainerName : containerName;

        // Create a full blob path which includes the container name
        var fullPath = $"{containerName}/{blobName}";

        // Validate the blob name and path using existing validation (updated to check full path length etc.)
        fullPath = ValidateBlobName(fullPath);

        if (dataStream.Length == 0)
            throw new InvalidOperationException("Attempting to write an empty stream to blob storage.");


        if (!dataStream.CanSeek)
        {
            var memoryStream = new MemoryStream();
            await dataStream.CopyToAsync(memoryStream).ConfigureAwait(false);
            memoryStream.Position = 0;
            dataStream = memoryStream; // Now use this memoryStream for the operation
        }

        // Reset the stream position to ensure all data is written
        dataStream.Position = 0;

        // Use the interface's WriteAsync method correctly
        await _blobStorage.WriteAsync(fullPath, dataStream, false, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task<Stream> ReadAsync(string blobName, string containerName)
    {
        // Ensure the container name defaults if not provided
        containerName = string.IsNullOrEmpty(containerName) ? _defaultContainerName : containerName;

        // Create a full blob path which includes the container name
        var fullPath = $"{containerName}/{blobName}";

        // Validate the blob name and path using existing validation (updated to check full path length etc.)
        fullPath = ValidateBlobName(fullPath);

        // Get a memory stream from the manager
        var stream = _memoryStreamManager.GetStream();

        // Use the correct method to open a read stream
        var readStream = await _blobStorage.OpenReadAsync(fullPath, CancellationToken.None).ConfigureAwait(false);

        if (readStream is null)
            throw new FileNotFoundException("Blob not found or no access.", fullPath);

        // Copy data from the blob stream to the managed memory stream
        await readStream.CopyToAsync(stream).ConfigureAwait(false);
        stream.Position = 0; // Reset the position for subsequent read operations

        return stream;
    }

    public async Task UpdateBlobAsync(string blobName, Stream newDataStream, string containerName)
    {
        containerName = string.IsNullOrEmpty(containerName) ? _defaultContainerName : containerName;
        var fullPath = $"{containerName}/{blobName}";

        // Check if the blob exists
        var exists = await _blobStorage.ExistsAsync(new[] { fullPath }, CancellationToken.None).ConfigureAwait(false);
        if (!exists.FirstOrDefault())
            throw new FileNotFoundException("The specified blob does not exist.", fullPath);

        // Delete existing blob
        await _blobStorage.DeleteAsync(new[] { fullPath }, CancellationToken.None).ConfigureAwait(false);

        // Upload the new data
        await _blobStorage.WriteAsync(fullPath, newDataStream, false, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string blobName, string containerName)
    {
        containerName = string.IsNullOrEmpty(containerName) ? _defaultContainerName : containerName;
        var fullPath = $"{containerName}/{blobName}";
        await _blobStorage.DeleteAsync(new[] { fullPath }, CancellationToken.None).ConfigureAwait(false);
    }

    private static string ValidateBlobName(string blobName)
    {
        if (string.IsNullOrEmpty(blobName))
            throw new ArgumentException("Blob name cannot be null or empty.", nameof(blobName));
        if (blobName.Length > 1024)
            throw new ArgumentException("Blob name cannot exceed 1024 characters.", nameof(blobName));
        return blobName;
    }
}
