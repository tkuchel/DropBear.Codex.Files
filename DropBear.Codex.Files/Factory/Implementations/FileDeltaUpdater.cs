using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using FastRsync.Delta;
using FastRsync.Diagnostics;
using FastRsync.Signature;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using ZLogger;
using ILoggerFactory = DropBear.Codex.AppLogger.Interfaces.ILoggerFactory;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileDeltaUpdater : IFileDeltaUtility
{
    private readonly ILogger<FileDeltaUpdater>? _logger;
    private readonly ConsoleProgressReporter _progressReporter = new();
    private readonly RecyclableMemoryStreamManager? _streamManager;

    public FileDeltaUpdater(RecyclableMemoryStreamManager? streamManager, ILoggerFactory? loggerFactory)
    {
        _streamManager = streamManager;
        _logger = loggerFactory?.CreateLogger<FileDeltaUpdater>();
    }


    public async Task<Result<byte[]>> CalculateFileSignatureAsync(byte[]? basisFileData)
    {
        if (basisFileData is null || basisFileData.Length is 0)
        {
            _logger?.ZLogWarning($"Attempted to calculate a file signature with empty basis file data.");
            return Result<byte[]>.Failure("Basis file data cannot be empty.");
        }

        if (_streamManager is null)
        {
            _logger?.ZLogError($"Stream manager is not available.");
            return Result<byte[]>.Failure("Internal error: Stream management service is unavailable.");
        }

        try
        {
            using var basisFileStream = _streamManager.GetStream("basisFile", basisFileData, 0, basisFileData.Length);
            using var signatureStream = _streamManager.GetStream("signatureFile");
            var signatureBuilder = new SignatureBuilder();

            await signatureBuilder.BuildAsync(basisFileStream, new SignatureWriter(signatureStream))
                .ConfigureAwait(false);

            signatureStream.Position = 0;
            return Result<byte[]>.Success(signatureStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger?.ZLogError(ex, $"Failed to calculate file signature due to an unexpected error.");
            return Result<byte[]>.Failure("Unexpected error occurred while calculating file signature.");
        }
    }

    public async Task<Result<byte[]>> CalculateDeltaBetweenBasisFileAndNewFileAsync(byte[]? signatureFileData,
        byte[]? newFileData)
    {
        if (signatureFileData is null || signatureFileData.Length is 0)
            return Result<byte[]>.Failure("Signature file data cannot be empty.");

        if (newFileData is null || newFileData.Length is 0)
            return Result<byte[]>.Failure("New file data cannot be empty.");

        if (_streamManager is null)
            return Result<byte[]>.Failure(
                "Stream management service is unavailable. Please check the system configuration.");

        try
        {
            using var deltaStream = _streamManager.GetStream("deltaStream");
            using var newFileStream = _streamManager.GetStream("newFile", newFileData, 0, newFileData.Length);
            using var signatureStream =
                _streamManager.GetStream("signatureFile", signatureFileData, 0, signatureFileData.Length);

            var signatureReader = new SignatureReader(signatureStream, _progressReporter);
            var deltaBuilder = new DeltaBuilder();
            var deltaWriter = new BinaryDeltaWriter(deltaStream);

            await deltaBuilder.BuildDeltaAsync(newFileStream, signatureReader, deltaWriter).ConfigureAwait(false);

            deltaStream.Position = 0;
            return Result<byte[]>.Success(deltaStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger?.ZLogError(ex, $"Failed to calculate the delta between basis and new file.");
            return Result<byte[]>.Failure("An unexpected error occurred while calculating the delta.");
        }
    }


    public async Task<Result<byte[]>> ApplyDeltaToBasisFileAsync(byte[]? basisFileData, byte[]? deltaFileData)
    {
        if (basisFileData is null || basisFileData.Length is 0 || deltaFileData is null || deltaFileData.Length is 0)
            return Result<byte[]>.Failure("Input file data cannot be empty.");

        if (_streamManager is null)
            return Result<byte[]>.Failure("Stream management service is unavailable.");

        try
        {
            using var deltaStream = _streamManager.GetStream("deltaStream", deltaFileData, 0, deltaFileData.Length);
            using var basisFileStream = _streamManager.GetStream("basisFile", basisFileData, 0, basisFileData.Length);
            using var resultStream = _streamManager.GetStream("resultStream");

            var deltaApplier = new DeltaApplier { SkipHashCheck = true };
            await deltaApplier
                .ApplyAsync(basisFileStream, new BinaryDeltaReader(deltaStream, _progressReporter), resultStream)
                .ConfigureAwait(false);

            resultStream.Position = 0;
            return Result<byte[]>.Success(resultStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger?.ZLogError(ex, $"Failed to apply delta to the basis file.");
            return Result<byte[]>.Failure("Failed to apply delta.");
        }
    }
}
