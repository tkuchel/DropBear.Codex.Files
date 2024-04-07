using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ZLogger;
using ILoggerFactory = DropBear.Codex.AppLogger.Interfaces.ILoggerFactory;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileUpdater : IFileUpdater, IDisposable
{
    private static RecyclableMemoryStreamManager? s_streamManager;
    private readonly ILogger<FileCreator> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private bool _disposed;
    private bool _useDeltaEncoding;
    private bool _useJsonSerialization;

    public FileUpdater(RecyclableMemoryStreamManager? streamManager, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        s_streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));

        // Create a logger instance
        _logger = _loggerFactory.CreateLogger<FileCreator>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IFileUpdater WithJsonSerialization()
    {
        _useJsonSerialization = true;
        return this;
    }

    public IFileUpdater WithMessagePackSerialization()
    {
        _useJsonSerialization = false;
        return this;
    }

    public async Task<Result> UpdateFileAsync(string filePath, DropBearFile newContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (directoryPath is null) throw new InvalidOperationException("Invalid file path.");

            // Create the directory if it does not exist
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            // Read the existing file into memory
            var existingFile = await FileManagerFactory.FileReader().ReadFileAsync(filePath, cancellationToken)
                .ConfigureAwait(false);

            if (existingFile.IsFailure)
                return Result.Failure(existingFile.ErrorMessage);

            // Write existing file into a byte array for comparison
            var existingFileByteArray = await FileManagerFactory.FileWriter()
                .WriteFileToByteArrayAsync(existingFile.Value).ConfigureAwait(false);

            if (existingFileByteArray.IsFailure)
                return Result.Failure(existingFileByteArray.ErrorMessage);

            // Delete the existing file if it exists
            await FileManagerFactory.FileDeleter().DeleteFileAsync(filePath).ConfigureAwait(false);

            if (_useDeltaEncoding)
            {
                // Get an instance of FileManagerFactory FileDeltaUtility
                var fileDeltaUtility = FileManagerFactory.FileDeltaUtility();

                // Calculate the file signature
                var fileSignature = await fileDeltaUtility.CalculateFileSignatureAsync(existingFileByteArray.Value)
                    .ConfigureAwait(false);

                if (fileSignature.IsFailure)
                    return Result.Failure(fileSignature.ErrorMessage);

                var fileContent = await FileManagerFactory.FileWriter().WriteFileToByteArrayAsync(newContent)
                    .ConfigureAwait(false);

                if (fileContent.IsFailure)
                    return Result.Failure(fileContent.ErrorMessage);

                // Calculate the delta between the basis file and the new file
                var delta = await fileDeltaUtility
                    .CalculateDeltaBetweenBasisFileAndNewFileAsync(fileSignature.Value, fileContent.Value)
                    .ConfigureAwait(false);

                if (delta.IsFailure)
                    return Result.Failure(delta.ErrorMessage);

                // Apply delta to the basis file byte array
                var updatedFile = await fileDeltaUtility
                    .ApplyDeltaToBasisFileAsync(existingFileByteArray.Value, delta.Value).ConfigureAwait(false);

                if (updatedFile.IsFailure)
                    return Result.Failure(updatedFile.ErrorMessage);

                // Write the updated file to the filepath
                var writeResult = await WriteBytesToFileAsync(updatedFile.Value, newContent.Metadata.FileName, filePath)
                    .ConfigureAwait(false);

                if (writeResult.IsFailure)
                    return Result.Failure(writeResult.ErrorMessage);
            }
            else
            {
                // Write the new file to the filepath
                var writeResult = await WriteFileAsync(newContent, filePath).ConfigureAwait(false);

                if (writeResult.IsFailure)
                    return Result.Failure(writeResult.ErrorMessage);
            }

            _logger.ZLogInformation($"File updated successfully: {filePath}");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"Error updating file: {filePath}. Exception: {ex.Message}");
            return Result.Failure(ex.Message);
        }
    }

    public FileUpdater WithDeltaEncoding()
    {
        _useDeltaEncoding = true;
        return this;
    }

    private async Task<Result> WriteFileAsync(DropBearFile file, string filePath)
    {
        try
        {
            // Get an instance of FileManagerFactory FileWriter
            var fileWriter = _useJsonSerialization ? FileManagerFactory.FileWriter().WithJsonSerialization() : FileManagerFactory.FileWriter().WithMessagePackSerialization();

            // Write the new file to the filepath.
            var writeResult = await fileWriter.WriteFileAsync(file, filePath).ConfigureAwait(false);

            if (writeResult.IsFailure)
                return Result.Failure(writeResult.ErrorMessage);

            _logger.ZLogInformation($"File written successfully: {filePath}");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"Error writing DropBear file with RecyclableMemoryStream: {ex.Message}");
            return Result.Failure(ex.Message);
        }
    }

    private async Task<Result> WriteBytesToFileAsync(byte[] bytes, string fileName, string filePath)
    {
        try
        {
            // Get an instance of FileManagerFactory FileWriter
            var fileWriter = _useJsonSerialization ? FileManagerFactory.FileWriter().WithJsonSerialization() : FileManagerFactory.FileWriter().WithMessagePackSerialization();

            // Write the new file to the filepath.
            var writeResult =
                await fileWriter.WriteByteArrayToFileAsync(bytes, fileName, filePath).ConfigureAwait(false);

            if (writeResult.IsFailure)
                return Result.Failure(writeResult.ErrorMessage);

            _logger.ZLogInformation($"File written successfully: {filePath}");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"Error writing DropBear file with RecyclableMemoryStream: {ex.Message}");
            return Result.Failure(ex.Message);
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            // Dispose managed state (managed objects).
            if (_loggerFactory is IDisposable disposable)
                disposable.Dispose();

        _disposed = true;
    }
}
