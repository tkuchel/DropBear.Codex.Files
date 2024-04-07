using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using Microsoft.Extensions.Logging;
using ZLogger;
using ILoggerFactory = DropBear.Codex.AppLogger.Interfaces.ILoggerFactory;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileDeleter : IFileDeleter
{
    private readonly ILogger<FileDeleter> _logger;

    public FileDeleter(ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _logger = loggerFactory.CreateLogger<FileDeleter>();
    }

    public async Task<Result> DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.ZLogWarning($"Attempted to delete a file with a null or empty path.");
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        try
        {
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath)).ConfigureAwait(false);
                _logger.ZLogInformation($"File deleted successfully: {filePath}");
                return Result.Success();
            }
            else
            {
                _logger.ZLogWarning($"File not found: {filePath}");
                return Result.Failure("File not found.");
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Error deleting file: {filePath}");
            return Result.Failure(ex.Message);
        }
    }
}
