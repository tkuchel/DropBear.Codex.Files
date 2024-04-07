using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using FastRsync.Delta;
using FastRsync.Diagnostics;
using FastRsync.Signature;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ZLogger;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileDeltaUtility : IFileDeltaUtility, IDisposable
{
    private readonly ILogger<FileDeltaUtility> _logger;
    private readonly RecyclableMemoryStreamManager _streamManager;
    private bool _disposed;

    public FileDeltaUtility(RecyclableMemoryStreamManager streamManager, ILogger<FileDeltaUtility> logger)
    {
        _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task<Result<byte[]>> CalculateFileSignatureAsync(byte[]? basisFileData)
    {
        if (basisFileData == null || basisFileData.Length == 0) return Result<byte[]>.Failure("EmptyBasisFileData");

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
            _logger.ZLogError(ex, $"Failed to calculate file signature.");
            return Result<byte[]>.Failure("FailedToCalculateFileSignature");
        }
    }

    public async Task<Result<byte[]>> CalculateDeltaBetweenBasisFileAndNewFileAsync(byte[]? signatureFileData,
        byte[]? newFileData)
    {
        if (signatureFileData == null || signatureFileData.Length == 0 || newFileData == null ||
            newFileData.Length == 0) return Result<byte[]>.Failure("EmptyFileData");

        try
        {
            using var deltaStream = _streamManager.GetStream("deltaStream");
            using var newFileStream = _streamManager.GetStream("newFile", newFileData, 0, newFileData.Length);
            using var signatureStream =
                _streamManager.GetStream("signatureFile", signatureFileData, 0, signatureFileData.Length);

            var progressReporter = new ConsoleProgressReporter();
            var signatureReader = new SignatureReader(signatureStream, progressReporter);
            var deltaBuilder = new DeltaBuilder();
            var deltaWriter = new BinaryDeltaWriter(deltaStream);

            await deltaBuilder.BuildDeltaAsync(newFileStream, signatureReader, deltaWriter).ConfigureAwait(false);

            deltaStream.Position = 0;
            return Result<byte[]>.Success(deltaStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Failed to calculate delta.");
            return Result<byte[]>.Failure("FailedToCalculateDelta");
        }
    }

    public async Task<Result<byte[]>> ApplyDeltaToBasisFileAsync(byte[]? basisFileData, byte[]? deltaFileData)
    {
        if (basisFileData == null || basisFileData.Length == 0 || deltaFileData == null || deltaFileData.Length == 0)
            return Result<byte[]>.Failure("EmptyFileData");

        try
        {
            using var deltaStream = _streamManager.GetStream("deltaStream", deltaFileData, 0, deltaFileData.Length);
            using var basisFileStream = _streamManager.GetStream("basisFile", basisFileData, 0, basisFileData.Length);
            using var resultStream = _streamManager.GetStream("resultStream");

            var progressReporter = new ConsoleProgressReporter();
            var deltaApplier = new DeltaApplier { SkipHashCheck = true };

            await deltaApplier
                .ApplyAsync(basisFileStream, new BinaryDeltaReader(deltaStream, progressReporter), resultStream)
                .ConfigureAwait(false);

            resultStream.Position = 0;
            return Result<byte[]>.Success(resultStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Failed to apply delta.");
            return Result<byte[]>.Failure("FailedToApplyDelta");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Managed resource clean-up if needed.
            }

            _disposed = true;
        }
    }
}
