using System.Collections.ObjectModel;
using System.Security.Cryptography;
using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ZLogger;
using ILoggerFactory = DropBear.Codex.AppLogger.Interfaces.ILoggerFactory;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileReader : IFileReader, IDisposable
{
    private const int HashSize = 32;
    private static RecyclableMemoryStreamManager? s_streamManager;

    private static bool s_useJsonSerialization = true;

    private static ILogger<FileReader>? s_logger;
    private readonly ILoggerFactory _loggerFactory;
    private bool _disposed;

    public FileReader(RecyclableMemoryStreamManager? streamManager, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        s_streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        // Create a logger instance for MyClass
        s_logger = _loggerFactory.CreateLogger<FileReader>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IFileReader WithJsonSerialization(bool fileDatWasSerializedToJson)
    {
        s_useJsonSerialization = fileDatWasSerializedToJson;
        return this;
    }

    public IFileReader WithMessagePackSerialization(bool fileDataWasSerializedToMessagePack)
    {
        s_useJsonSerialization = !fileDataWasSerializedToMessagePack;
        return this;
    }

    public async Task<Result<DropBearFile>> ReadFileAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Sanitize the file path
            filePath = Path.GetFullPath(filePath);

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                s_logger?.ZLogError($"File {filePath} does not exist");
                return Result<DropBearFile>.Failure($"File {filePath} does not exist");
            }

            // Read the file


            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Read the file


            await using (fileStream.ConfigureAwait(false))
            {
                // Instead of reading the entire file into a standard MemoryStream,
                // use a RecyclableMemoryStream for the file reading process.
                using (var memoryStream = s_streamManager.GetStream("FileReader"))
                {
                    // Copy the FileStream to the RecyclableMemoryStream.
                    await fileStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

                    // After copying, reset the position of the memory stream to start reading from the beginning.
                    memoryStream.Position = 0;

                    // Read components and perform file integrity verification within the memory stream.
                    var readResult = await ReadComponentsAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                    if (!readResult.IsSuccess)
                    {
                        s_logger?.ZLogError($"{readResult.ErrorMessage}");
                        return Result<DropBearFile>.Failure(readResult.ErrorMessage);
                    }

                    var verificationResult =
                        await VerifyFileIntegrityAsync(memoryStream, readResult.Components, cancellationToken)
                            .ConfigureAwait(false);
                    if (!verificationResult)
                    {
                        s_logger?.ZLogError($"File hash verification failed.");
                        return Result<DropBearFile>.Failure("File hash verification failed.");
                    }

                    s_logger?.ZLogInformation($"File hash verification succeeded.");


                    s_logger?.ZLogInformation($"DropBear file read successfully with RecyclableMemoryStream.");
                    return DeserializeDropBearFile(readResult.Components, cancellationToken);
                }
            }
        }
        catch (Exception? e)
        {
            s_logger?.ZLogError(e, $"Error reading file {filePath}");
            return Result<DropBearFile>.Failure(e.Message);
        }
    }

    private static Result<DropBearFile> DeserializeDropBearFile(IList<byte[]> components,
        CancellationToken cancellationToken)
    {
        try
        {
            // Assume a method exists on DropBearFile that can reconstruct it from its components
            var file = DropBearFile.Reconstruct(s_useJsonSerialization, new Collection<byte[]>(components),
                cancellationToken);
            var actualSignature = file.Header?.Signature.Signature.ToArray();
            return actualSignature is not null && !VerifyFileSignature(actualSignature)
                ? Result<DropBearFile>.Failure("File signature verification failed.")
                : Result<DropBearFile>.Success(file);
        }
        catch (Exception ex)
        {
            return Result<DropBearFile>.Failure($"Deserialization failed: {ex.Message}");
        }
    }

    private static bool VerifyHash(byte[] computedHash, byte[] expectedHash) =>
        computedHash.AsSpan().SequenceEqual(expectedHash.AsSpan());

    private static async Task<(bool IsSuccess, List<byte[]> Components, string ErrorMessage)> ReadComponentsAsync(
        Stream fileStream, CancellationToken cancellationToken)
    {
        var components = new List<byte[]>();
        try
        {
            while (fileStream.Position < fileStream.Length - HashSize)
            {
                var component = await ReadWithLengthPrefixAsync(fileStream, cancellationToken).ConfigureAwait(false);
                components.Add(component);
            }

            return (true, components, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, components, $"Error reading components: {ex.Message}");
        }
    }

    private static async Task<bool> VerifyFileIntegrityAsync(
        Stream fileStream, IEnumerable<byte[]> components, CancellationToken cancellationToken)
    {
        try
        {
            // Reconstruct the stream to include LPE bytes, if necessary
            var reconstructedStream = ReconstructStreamWithLPE(components);

            // Compute hash on the reconstructed stream
            var computedHash = await ComputeHashAsync(reconstructedStream, cancellationToken)
                .ConfigureAwait(false);

            // Rewind the fileStream to ensure it can be read from the beginning
            if (fileStream.CanSeek) fileStream.Seek(0, SeekOrigin.Begin);

            // Read the expected hash from the end of the original file stream
            var expectedHash = await ReadHashFromEndAsync(fileStream, HashSize, cancellationToken)
                .ConfigureAwait(false);

            return VerifyHash(computedHash, expectedHash);
        }
        catch
        {
            // Consider logging the exception or specific handling here
            return false;
        }
    }

    private static async Task<byte[]> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (!stream.CanRead)
            throw new InvalidOperationException("Stream must be readable to compute the hash.");

        using var hasher = SHA256.Create();
        byte[] hash;
        try
        {
            hash = await hasher.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw; // Rethrow cancellation exceptions without wrapping.
        }
        catch (Exception ex)
        {
            // Consider logging here if applicable.
            throw new InvalidOperationException($"An error occurred while computing the hash: {ex.Message}", ex);
        }

        return hash;
    }


    private static async Task<byte[]> ReadWithLengthPrefixAsync(Stream stream,
        CancellationToken cancellationToken = default)
    {
        if (!stream.CanRead) throw new InvalidOperationException("Stream must be readable.");

        if (stream.Length - stream.Position < sizeof(int))
            throw new EndOfStreamException("Not enough bytes in the stream for a length prefix.");

        var lengthPrefix = new byte[sizeof(int)];
        await ReadExactAsync(stream, lengthPrefix, cancellationToken).ConfigureAwait(false);

        var dataLength = BitConverter.ToInt32(lengthPrefix, 0);
        if (dataLength < 0 || stream.Length - stream.Position < dataLength)
            throw new InvalidOperationException(
                "Invalid data length or not enough bytes in the stream for the declared data length.");

        var data = new byte[dataLength];
        await ReadExactAsync(stream, data, cancellationToken).ConfigureAwait(false);

        return data;
    }

    private static bool VerifyFileSignature(byte[] actualSignature)
    {
        try
        {
            var expectedSignature = new FileSignature().Signature; // Assuming this is ReadOnlyCollection<byte>
            var expectedSignatureArray = expectedSignature.ToArray(); // Convert to array

            // Console.WriteLine for debugging purposes
            // Console.WriteLine(BitConverter.ToString(actualSignature));
            // Console.WriteLine(BitConverter.ToString(expectedSignatureArray));

            return actualSignature.AsSpan().SequenceEqual(expectedSignatureArray.AsSpan());
        }
        catch (Exception ex)
        {
            s_logger?.ZLogError($"Error verifying file signature: {ex.Message}");
            return false;
        }
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        // Check if the stream supports reading.
        if (!stream.CanRead) throw new InvalidOperationException("Stream must be readable to perform read operations.");

        var totalBytesRead = 0;
        var bytesToRead = buffer.Length;

        while (totalBytesRead < bytesToRead)
        {
            var bytesRead = await stream.ReadAsync(
                buffer.AsMemory(totalBytesRead, bytesToRead - totalBytesRead),
                cancellationToken).ConfigureAwait(false);

            if (bytesRead is 0)
                // The stream ended unexpectedly, which might indicate a corrupted or incomplete stream.
                throw new EndOfStreamException(
                    $"The stream ended before all the data could be read. Total bytes read: {totalBytesRead}, Expected: {bytesToRead}.");

            totalBytesRead += bytesRead;
        }
    }

    private static async Task<byte[]> ReadHashFromEndAsync(Stream stream, int hashSize,
        CancellationToken cancellationToken = default)
    {
        if (!stream.CanSeek) throw new InvalidOperationException("Stream must support seeking.");

        if (stream.Length < hashSize) throw new InvalidOperationException("Stream is shorter than expected hash size.");

        stream.Seek(-hashSize, SeekOrigin.End);
        var hash = new byte[hashSize];
        await ReadExactAsync(stream, hash, cancellationToken).ConfigureAwait(false);

        return hash;
    }

    // ReSharper disable once InconsistentNaming
    private static MemoryStream ReconstructStreamWithLPE(IEnumerable<byte[]> components)
    {
        var memoryStream = new MemoryStream();

        // Example: Assuming LPE involves prefixing each component with its length
        foreach (var component in components)
        {
            // Example: Convert component length to bytes and write to stream (adjust based on actual LPE format)
            var lengthPrefix = BitConverter.GetBytes(component.Length);
            memoryStream.Write(lengthPrefix, 0, lengthPrefix.Length);

            // Write the actual component
            memoryStream.Write(component, 0, component.Length);
        }

        // Rewind the stream to allow reading from the beginning
        memoryStream.Position = 0;

        return memoryStream;
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
