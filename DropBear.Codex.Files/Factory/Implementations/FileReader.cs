using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ZLogger;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileReader : IFileReader, IDisposable
{
    private const int HashSize = 32;
    private static bool _useJsonSerialization = true;
    private readonly ILogger<FileReader> _logger;
    private readonly RecyclableMemoryStreamManager _streamManager;
    private bool _disposed;

    public FileReader(RecyclableMemoryStreamManager streamManager, ILogger<FileReader> logger)
    {
        _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IFileReader WithJsonSerialization(bool fileDataWasSerializedToJson)
    {
        _useJsonSerialization = fileDataWasSerializedToJson;
        return this;
    }

    public IFileReader WithMessagePackSerialization(bool fileDataWasSerializedToMessagePack)
    {
        _useJsonSerialization = !fileDataWasSerializedToMessagePack;
        return this;
    }

    public async Task<Result<DropBearFile>> ReadFileAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.ZLogError($"File path cannot be null or whitespace.");
            return Result<DropBearFile>.Failure("InvalidFilePath");
        }

        filePath = Path.GetFullPath(filePath);

        if (!File.Exists(filePath))
        {
            _logger.ZLogError($"File does not exist: {filePath}");
            return Result<DropBearFile>.Failure($"File does not exist: {filePath}");
        }

        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var memoryStream = _streamManager.GetStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            memoryStream.Position = 0;

            var readResult = await ReadComponentsAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            if (!readResult.IsSuccess)
            {
                _logger.ZLogError($"{readResult.ErrorMessage}");
                return Result<DropBearFile>.Failure(readResult.ErrorMessage);
            }

            var verificationResult =
                await VerifyFileIntegrityAsync(memoryStream, readResult.Components, cancellationToken)
                    .ConfigureAwait(false);
            if (!verificationResult)
            {
                _logger.ZLogError($"File hash verification failed.");
                return Result<DropBearFile>.Failure("File hash verification failed.");
            }

            _logger.ZLogInformation($"File hash verification succeeded.");
            _logger.ZLogInformation($"DropBear file read successfully with RecyclableMemoryStream.");
            return DeserializeDropBearFile(readResult.Components, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Failed to read file: {FilePath}", filePath);
            return Result<DropBearFile>.Failure($"Failed to read file: {ex.Message}");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Dispose managed state (managed objects) if needed
            // For instance: _streamManager.Dispose() if it implements IDisposable
        }

        _disposed = true;
    }
}
