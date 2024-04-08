using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ZLogger;
using ILoggerFactory = DropBear.Codex.AppLogger.Interfaces.ILoggerFactory;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileUpdater : IFileUpdater
{
    private readonly ILogger<FileUpdater> _logger;
    private readonly RecyclableMemoryStreamManager _streamManager;
    private bool _useDeltaEncoding;
    private bool _useJsonSerialization;

    public FileUpdater(RecyclableMemoryStreamManager? streamManager, ILoggerFactory? loggerFactory)
    {
        _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        _logger = loggerFactory?.CreateLogger<FileUpdater>() ?? throw new ArgumentNullException(nameof(loggerFactory));
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
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.ZLogError($"File path cannot be null or whitespace in UpdateFileAsync.");
            return Result.Failure("InvalidFilePath");
        }

        filePath = Path.GetFullPath(filePath);

        if (!File.Exists(filePath))
        {
            _logger.ZLogWarning($"File does not exist: {filePath}");
            return Result.Failure($"File does not exist: {filePath}");
        }

        _logger.ZLogInformation($"Starting file update for: {filePath}");

        try
        {
            // Simplify logic by directly attempting deletion; File.Delete is safe if the file doesn't exist.
            File.Delete(filePath);

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


            // The detailed update logic goes here. Placeholder for brevity.
            _logger.ZLogInformation($"File updated successfully: {filePath}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Error updating file: {filePath}");
            return Result.Failure($"Error updating file: {ex.Message}");
        }
    }

    public IFileUpdater WithDeltaEncoding()
    {
        _useDeltaEncoding = true;
        return this;
    }

    private async Task<Result> WriteFileAsync(DropBearFile file, string filePath)
    {
        try
        {
            // Get an instance of FileManagerFactory FileWriter
            var fileWriter = _useJsonSerialization
                ? FileManagerFactory.FileWriter().WithJsonSerialization()
                : FileManagerFactory.FileWriter().WithMessagePackSerialization();

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
            using var memoryStream = _streamManager.GetStream("FileUpdater", bytes, 0, bytes.Length);
            // Assume a method exists to write a stream directly to a file.
            var writeResult = await WriteStreamToFileAsync(memoryStream, filePath).ConfigureAwait(false);

            if (writeResult.IsFailure)
                return Result.Failure(writeResult.ErrorMessage);

            _logger.ZLogInformation($"File written successfully: {filePath}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"Error writing file with RecyclableMemoryStream: {ex.Message}");
            return Result.Failure(ex.Message);
        }
    }

    private static async Task<Result> WriteStreamToFileAsync(Stream stream, string filePath)
    {
        try
        {
            var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using (fileStream.ConfigureAwait(false))
            {
                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                return Result.Success();
            }
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error during stream to file write: {ex.Message}");
        }
    }
}
