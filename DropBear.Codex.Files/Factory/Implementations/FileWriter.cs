using System.Security.Cryptography;
using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Utils;
using DropBear.Codex.Utilities.Helpers;
using MessagePack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ServiceStack.Text;
using ZLogger;
using ILoggerFactory = DropBear.Codex.AppLogger.Interfaces.ILoggerFactory;


namespace DropBear.Codex.Files.Factory.Implementations;

public class FileWriter : IFileWriter
{
    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

    private readonly ILogger<FileWriter> _logger;
    private readonly RecyclableMemoryStreamManager _streamManager;
    private bool _useJsonSerialization = true;


    public FileWriter(RecyclableMemoryStreamManager? streamManager, ILoggerFactory? loggerFactory)
    {
        _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        _logger = loggerFactory?.CreateLogger<FileWriter>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }


    public async Task<Result> WriteByteArrayToFileAsync(byte[] bytes, string fileName, string filePath)
    {
        try
        {
            // Validate file path
            if (FileUtility.IsValidFileName(filePath))
                return Result.Failure("File path cannot be null or empty");
// Construct the full file path and name
            var fullFilePathAndNameWithExtension = FileUtility.GetFilePath(fileName, filePath);


// Check if the stream manager is null
            if (_streamManager is null)
                throw new InvalidOperationException("Stream manager cannot be null");

// Create a stream to write the file
            using var memoryStream = _streamManager.GetStream("FileWriter");
            await memoryStream.WriteAsync(bytes).ConfigureAwait(false);

// Reset the stream position 
            memoryStream.Position = 0;

// Write from the memory stream to the actual file.
            var fileStream =
                new FileStream(fullFilePathAndNameWithExtension, FileMode.Create, FileAccess.Write, FileShare.None);

// Write from the memory stream to the actual file.
            await using (fileStream.ConfigureAwait(false))
            {
                await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);

                _logger.ZLogInformation($"Successfully wrote file to {fullFilePathAndNameWithExtension}");
                return Result.Success();
            }
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"Failed to write file to {filePath}");
            return Result.Failure(e.Message);
        }
    }

    public IFileWriter WithJsonSerialization()
    {
        _useJsonSerialization = true;
        return this;
    }

    public IFileWriter WithMessagePackSerialization()
    {
        _useJsonSerialization = false;
        return this;
    }

    public async Task<Result> WriteFileAsync(DropBearFile file, string filePath)
    {
        try
        {
            // Validate file path
            if (!FileUtility.IsValidFileName(filePath))
                return Result.Failure("File path cannot be null or empty");

            // Check if the directory exists
            var fullFilePathAndName = FileUtility.GetFilePath(file, filePath);

            // Check if the stream manager is null
            if (_streamManager is null)
                throw new InvalidOperationException("Stream manager cannot be null");

            // Create a stream to write the file
            using var memoryStream = _streamManager.GetStream("FileWriter");
            await SerializeDropBearFileComponents(file, memoryStream).ConfigureAwait(false);

            // After serializing the file components into the memory stream, compute and append the hash.
            memoryStream.Position = 0; // Ensure the stream is at the beginning before computing the hash.
            var fileHash = await ComputeAndAppendHashAsync(memoryStream).ConfigureAwait(false);
            _logger.ZLogInformation($"Computed file hash: {BitConverter.ToString(fileHash)}");

            // Reset the stream position after computing the hash, before writing to the file.
            memoryStream.Position = 0;

            // Write from the memory stream to the actual file.
            var fileStream =
                new FileStream(fullFilePathAndName, FileMode.Create, FileAccess.Write, FileShare.None);

            // Write from the memory stream to the actual file.
            await using (fileStream.ConfigureAwait(false))
            {
                await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);

                _logger.ZLogInformation($"Successfully wrote file to {fullFilePathAndName}");
                return Result.Success();
            }
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"Failed to write file to {filePath}");
            return Result.Failure(e.Message);
        }
    }

    public async Task<Result<byte[]>> WriteFileToByteArrayAsync(DropBearFile file)
    {
        try
        {
            // Check if the stream manager is null
            if (_streamManager is null)
                throw new InvalidOperationException("Stream manager cannot be null");

            // Create a stream to write the file
            using var memoryStream = _streamManager.GetStream("FileWriter");
            await SerializeDropBearFileComponents(file, memoryStream).ConfigureAwait(false);

            // After serializing the file components into the memory stream, compute and append the hash.
            memoryStream.Position = 0; // Ensure the stream is at the beginning before computing the hash.
            var fileHash = await ComputeAndAppendHashAsync(memoryStream).ConfigureAwait(false);
            _logger.ZLogInformation($"Computed file hash: {BitConverter.ToString(fileHash)}");

            // Reset the stream position after computing the hash, before writing to the file.
            memoryStream.Position = 0;

            // Return the memory stream as a byte array
            return Result<byte[]>.Success(memoryStream.ToArray());
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"Failed to write file to byte array");
            return Result<byte[]>.Failure(e.Message);
        }
    }

    private static async Task<byte[]> ComputeAndAppendHashAsync(Stream stream,
        CancellationToken cancellationToken = default)
    {
        // Check if the stream supports writing.
        if (!stream.CanWrite) throw new InvalidOperationException("Stream must be writable to append the hash.");

        // Reset the position of the stream to the beginning for hashing.
        if (stream.CanSeek)
            stream.Position = 0;
        else
            throw new InvalidOperationException("Stream must support seeking to compute the hash from the beginning.");

        using var hasher = SHA256.Create();
        byte[] hash;

        try
        {
            // Compute the hash on the stream directly.
            hash = await hasher.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to compute hash: {ex.Message}", ex);
        }

        // Append the hash to the end of the stream.
        if (stream.CanSeek)
            stream.Position = stream.Length; // Go to the end to append the hash.
        else
            throw new InvalidOperationException("Stream must support seeking to append the hash at the end.");

        try
        {
            await stream.WriteAsync(hash, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to append hash: {ex.Message}", ex);
        }

        return hash; // Return the binary hash.
    }

    private async Task SerializeDropBearFileComponents(DropBearFile file, Stream fileStream)
    {
        // Serialize and write each component of DropBearFile with length prefix.
        await WriteComponentWithLengthPrefixAsync(fileStream, file.Header).ConfigureAwait(false);
        await WriteComponentWithLengthPrefixAsync(fileStream, file.Metadata).ConfigureAwait(false);
        await WriteComponentWithLengthPrefixAsync(fileStream, file.Content).ConfigureAwait(false);
    }

    private async Task WriteComponentWithLengthPrefixAsync<T>(Stream fileStream, T component)
    {
        // Check if the file stream supports writing.
        if (!fileStream.CanWrite)
            throw new InvalidOperationException("The stream must be writable to perform write operations.");

        try
        {
            // Serialize the component using MessagePack with provided options.
            var componentBytes = _useJsonSerialization
                ? JsonConvert.SerializeObject(component).GetBytes()
                : MessagePackSerializer.Serialize(component, Options);

            // Write the serialized component bytes to the stream with a length prefix.
            await WriteWithLengthPrefixAsync(fileStream, componentBytes).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log or handle the error as appropriate for your application.
            // For instance, you might want to log the error or throw a custom exception to indicate serialization or writing failed.
            throw new InvalidOperationException($"Failed to serialize or write the component: {ex.Message}", ex);
        }
    }

    private static async Task WriteWithLengthPrefixAsync(Stream stream, byte[] data)
    {
        if (!stream.CanWrite) throw new InvalidOperationException("Stream must be writable.");

        // Convert the length of the data to a byte array and write it to the stream.
        var lengthPrefix = BitConverter.GetBytes(data.Length);
        await stream.WriteAsync(lengthPrefix).ConfigureAwait(false);
        await stream.WriteAsync(data).ConfigureAwait(false);
    }
}
