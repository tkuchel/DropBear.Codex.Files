using Microsoft.IO;

namespace DropBear.Codex.Files.StorageManagers;

public class LocalStorageManager 
{
    private readonly string _baseDirectory;
    private readonly RecyclableMemoryStreamManager _memoryStreamManager;

    public LocalStorageManager(RecyclableMemoryStreamManager memoryStreamManager, string baseDirectory = @"C:\Data")
    {
        _baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
        _memoryStreamManager = memoryStreamManager ?? throw new ArgumentNullException(nameof(memoryStreamManager));

        if (!Directory.Exists(_baseDirectory)) Directory.CreateDirectory(_baseDirectory);
    }

    public string GetBaseDirectory() => _baseDirectory;

    public async Task WriteAsync(string fileName, Stream dataStream, string? subDirectory = null)
    {
        var directoryPath = Path.Combine(_baseDirectory, subDirectory ?? string.Empty);
        var fullPath = Path.Combine(directoryPath, fileName);

        // Ensure directory exists
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);


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

        // Write data to the file
        var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

        // Write data to the file
        await using (fileStream.ConfigureAwait(false))
        {
            await dataStream.CopyToAsync(fileStream).ConfigureAwait(false);
        }
    }

    public async Task<Stream> ReadAsync(string fileName, string? subDirectory = null)
    {
        var directoryPath = Path.Combine(_baseDirectory, subDirectory ?? string.Empty);
        var fullPath = Path.Combine(directoryPath, fileName);

        if (!File.Exists(fullPath)) throw new FileNotFoundException("The specified file does not exist.", fullPath);

        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            FileOptions.Asynchronous);
        var memoryStream = _memoryStreamManager.GetStream(); // Get a recyclable memory stream

        await using (fileStream.ConfigureAwait(false))
        {
            await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);
        }

        memoryStream.Position = 0; // Reset the position for subsequent read operations
        return memoryStream;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task DeleteAsync(string fileName, string? subDirectory = null)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        var directoryPath = Path.Combine(_baseDirectory, subDirectory ?? string.Empty);
        var fullPath = Path.Combine(directoryPath, fileName);

        if (File.Exists(fullPath))
            File.Delete(fullPath);
        else
            throw new FileNotFoundException("The specified file does not exist.", fullPath);
    }

    public async Task UpdateAsync(string fileName, Stream newDataStream, string? subDirectory = null)
    {
        var directoryPath = Path.Combine(_baseDirectory, subDirectory ?? string.Empty);
        var fullPath = Path.Combine(directoryPath, fileName);

        // Ensure file exists before trying to update
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The specified file does not exist for update.", fullPath);

        // Delete existing file
        File.Delete(fullPath);

        // Write the new data
        var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);

        // Write the new data
        await using (fileStream.ConfigureAwait(false))
        {
            await newDataStream.CopyToAsync(fileStream).ConfigureAwait(false);
        }
    }
}
