using System.Security.Cryptography;
using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Utilities.Helpers;
using MessagePack;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ZLogger;
using ILoggerFactory = DropBear.Codex.AppLogger.Interfaces.ILoggerFactory;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileWriter : IFileWriter, IDisposable
{
    private static RecyclableMemoryStreamManager? s_streamManager;

    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

    private static bool s_useJsonSerialization = true;

    private readonly ILogger<FileWriter> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private bool _disposed;

    public FileWriter(RecyclableMemoryStreamManager? streamManager, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        s_streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        // Create a logger instance for MyClass
        _logger = _loggerFactory.CreateLogger<FileWriter>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task<Result> WriteFileAsync(DropBearFile file, string filePath)
    {
        try
        {
            // Validate file path
            if (string.IsNullOrWhiteSpace(filePath))
                return Result.Failure("File path cannot be null or empty");

            // Check if the directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                if (directory is not null)
                    Directory.CreateDirectory(directory);

            // Serialize the content to JSON or MessagePack
            if (s_streamManager is null)
                throw new InvalidOperationException("Stream manager cannot be null");

            var fullFilePathAndName = ConstructFilePath(file, filePath);

            // Create a stream to write the file
            using var memoryStream = s_streamManager.GetStream("FileWriter");
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

    public IFileWriter WithJsonSerialization(bool serializeToJson)
    {
        s_useJsonSerialization = serializeToJson;
        return this;
    }

    public IFileWriter WithMessagePackSerialization(bool serializeToMessagePack)
    {
        s_useJsonSerialization = !serializeToMessagePack;
        return this;
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

    private static string ConstructFilePath(DropBearFile file, string filePath) =>
        // Construct and return the full file path and name based on file metadata and header.
        Path.Combine(filePath, $"{file.Metadata.FileName}.{file.Header?.Signature.Extension}");

    private static async Task SerializeDropBearFileComponents(DropBearFile file, Stream fileStream)
    {
        // Serialize and write each component of DropBearFile with length prefix.
        await WriteComponentWithLengthPrefixAsync(fileStream, file.Header).ConfigureAwait(false);
        await WriteComponentWithLengthPrefixAsync(fileStream, file.Metadata).ConfigureAwait(false);
        await WriteComponentWithLengthPrefixAsync(fileStream, file.Content).ConfigureAwait(false);
    }

    private static async Task WriteComponentWithLengthPrefixAsync<T>(Stream fileStream, T component)
    {
        // Check if the file stream supports writing.
        if (!fileStream.CanWrite)
            throw new InvalidOperationException("The stream must be writable to perform write operations.");

        try
        {
            // Serialize the component using MessagePack with provided options.
            var componentBytes = s_useJsonSerialization
                ? JsonSerializer.SerializeToString(component).GetBytes()
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