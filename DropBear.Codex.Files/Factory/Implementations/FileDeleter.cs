using DropBear.Codex.Files.Interfaces;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileDeleter : IFileDeleter, IDisposable
{
    private readonly ILogger<FileDeleter> _logger;
    private bool _disposed;

    public FileDeleter(ILogger<FileDeleter> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.ZLogWarning($"Attempted to delete a file with a null or empty path.");
            return;
        }

        try
        {
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath)).ConfigureAwait(false);
                _logger.ZLogInformation($"File deleted successfully: {filePath}");
            }
            else
            {
                _logger.ZLogWarning($"File not found: {filePath}");
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Error deleting file: {filePath}");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Managed resource clean-up
            }

            _disposed = true;
        }
    }
}
