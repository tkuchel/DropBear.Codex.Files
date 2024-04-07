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

public class FileReader : IFileReader
{
    private readonly ILogger<FileReader> _logger;
    private readonly RecyclableMemoryStreamManager _streamManager;

    private HashAlgorithmName _hashAlgorithmName = HashAlgorithmName.SHA256;
    private int _hashSize = 32; // SHA256 produces a 256-bit hash (32 bytes)
    private bool _useJsonSerialization = true;

    public FileReader(RecyclableMemoryStreamManager? streamManager, ILoggerFactory? loggerFactory)
    {
        _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        _logger = loggerFactory?.CreateLogger<FileReader>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public IFileReader WithJsonSerialization(bool fileWasSerializedToJson)
    {
        _useJsonSerialization = fileWasSerializedToJson;
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
            _logger.ZLogError(ex, $"Failed to read file: {filePath}");
            return Result<DropBearFile>.Failure($"Failed to read file: {ex.Message}");
        }
    }

    public FileReader WithHashAlgorithm(HashAlgorithmName hashAlgorithmName, int hashSize)
    {
        _hashAlgorithmName = hashAlgorithmName;
        _hashSize = hashSize; // Now correctly tied to the instance
        return this;
    }

    private async Task<byte[]> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (!stream.CanRead)
            throw new InvalidOperationException("Stream must be readable to compute the hash.");

        using var hasher = IncrementalHash.CreateHash(_hashAlgorithmName);
        const int bufferSize = 4096; // Adjust the buffer size as needed
        var buffer = new byte[bufferSize];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            hasher.AppendData(buffer, 0, bytesRead);
        return hasher.GetHashAndReset();
    }

    private Result<DropBearFile> DeserializeDropBearFile(IList<byte[]> components,
        CancellationToken cancellationToken)
    {
        try
        {
            // Assume a method exists on DropBearFile that can reconstruct it from its components
            var file = DropBearFile.Reconstruct(_useJsonSerialization, new Collection<byte[]>(components),
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

    private async Task<(bool IsSuccess, List<byte[]> Components, string ErrorMessage)> ReadComponentsAsync(
        Stream fileStream, CancellationToken cancellationToken)
    {
        var components = new List<byte[]>();
        try
        {
            while (fileStream.Position < fileStream.Length - _hashSize)
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

    private async Task<bool> VerifyFileIntegrityAsync(
        Stream fileStream, IEnumerable<byte[]> components, CancellationToken cancellationToken)
    {
        try
        {
            var reconstructedStream = ReconstructStreamWithLPE(components);
            var computedHash = await ComputeHashAsync(reconstructedStream, cancellationToken).ConfigureAwait(false);

            // Adjust the stream position for reading the stored hash.
            if (fileStream.CanSeek) fileStream.Seek(-_hashSize, SeekOrigin.End);
            var storedHash = new byte[_hashSize];
            await fileStream.ReadAsync(storedHash.AsMemory(0, _hashSize), cancellationToken).ConfigureAwait(false);

            return VerifyHash(computedHash, storedHash);
        }
        catch (Exception ex)
        {
            _logger?.ZLogError(ex, $"Failed to verify file integrity.");
            return false;
        }
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

    private bool VerifyFileSignature(byte[] actualSignature)
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
            _logger.ZLogError($"Error verifying file signature: {ex.Message}");
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
}
