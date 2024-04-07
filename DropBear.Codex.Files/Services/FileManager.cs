using System.Collections.ObjectModel;
using System.Security.Cryptography;
using DropBear.Codex.AppLogger.Interfaces;
using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.MessagePackChecker;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Models.FileComponents.MainComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using DropBear.Codex.Files.Utils;
using DropBear.Codex.Files.Validation.Strategies;
using DropBear.Codex.Utilities.Helpers;
using DropBear.Codex.Utilities.MessageTemplates;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;
using MessagePack;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace DropBear.Codex.Files.Services;

/// <summary>
///     Service for managing DropBear files.
/// </summary>
public class FileManager : IFileManager
{
    #region Field and Property Definitions

    private const int HashSize = 32; // 256 bits

    private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
        .WithCompression(MessagePackCompression.Lz4BlockArray)
        .WithSecurity(MessagePackSecurity.UntrustedData);

    private readonly ILogger<FileManager> _logger;
    private readonly IMessageTemplateManager _messageTemplateManager;
    private readonly IStrategyValidator _strategyValidator;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager = new();

    #endregion

    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the FileManager class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="strategyValidator">The strategy validator instance.</param>
    /// <param name="messageTemplateManager">The message template manager instance.</param>
    public FileManager(ILogger<FileManager> logger, IStrategyValidator strategyValidator,
        IMessageTemplateManager messageTemplateManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _strategyValidator = strategyValidator ?? throw new ArgumentNullException(nameof(strategyValidator));
        _messageTemplateManager =
            messageTemplateManager ?? throw new ArgumentNullException(nameof(messageTemplateManager));

        // Assuming an asynchronous context is acceptable for your scenario
        Task.Run(InitializeAsync).Wait();
    }

    #region Initialization Methods

    private async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing FileManager...");
        await RegisterValidationStrategiesAsync().ConfigureAwait(false);
        await RegisterMessageTemplatesAsync().ConfigureAwait(false);

        var compatibilityResult = await CheckMessagePackCompatibilityAsync().ConfigureAwait(false);
        if (compatibilityResult.IsValid)
            _logger.LogInformation("MessagePack compatibility check passed.");
        else
            _logger.LogWarning("MessagePack compatibility check failed.");

        _logger.LogInformation("FileManager initialized.");
    }

    private Task RegisterValidationStrategiesAsync()
    {
        // Register validation strategies with the StrategyValidator service
        _logger.LogInformation("Registering validation strategies.");
        _strategyValidator.RegisterStrategy(new FileContentValidationStrategy());
        _strategyValidator.RegisterStrategy(new FileHeaderValidationStrategy());
        _strategyValidator.RegisterStrategy(new FileMetaDataValidationStrategy());
        _strategyValidator.RegisterStrategy(new DropBearFileValidationStrategy());
        _logger.LogInformation("Validation strategies registered successfully.");
        return Task.CompletedTask;
    }

    private Task RegisterMessageTemplatesAsync()
    {
        // Register message templates with the MessageTemplateManager service
        _logger.LogInformation("Registering message templates.");
        _messageTemplateManager.RegisterTemplates(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "TestTemplateId", "Test template id: {0}" }
        });
        _logger.LogInformation("Message templates registered successfully.");
        return Task.CompletedTask;
    }

    private Task<ValidationResult> CheckMessagePackCompatibilityAsync()
    {
        // Check each class used in the DropBearFile and its subcomponents for compatibility with MessagePack serialization
        _logger.LogInformation("Checking type compatibility.");
        var typesToCheck = new List<Type>
        {
            typeof(ContentContainer),
            typeof(ContentTypeInfo),
            typeof(FileSignature),
            typeof(FileContent),
            typeof(FileHeader),
            typeof(FileMetadata),
            typeof(DropBearFile)
        };
        var results = MessagePackCompatibilityAggregator.CheckTypes(typesToCheck);
        _logger.LogInformation("Type compatibility check completed.");

        if (results.FailedTypes.Count is 0) return Task.FromResult(ValidationResult.Success());

        var validationResult = ValidationResult.Success();
        foreach (var (type, reason) in results.FailedTypes)
        {
            validationResult.AddError(type, reason);
            _logger.LogError($"Type {type} failed compatibility check: {reason}");
        }

        return Task.FromResult(validationResult);
    }

    #endregion

    #endregion

    #region Public Methods

    /// <summary>
    ///     Creates a DropBear file asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of content to include in the file.</typeparam>
    /// <param name="name">The name of the file.</param>
    /// <param name="content">The content to include in the file.</param>
    /// <param name="compress">Whether to compress the file content.</param>
    /// <param name="contentType">The type of content (optional).</param>
    /// <param name="forceCreation">Whether to force the creation of the file even if validation fails.</param>
    /// <returns>The created DropBear file.</returns>
    public async Task<Result<DropBearFile>> CreateFileAsync<T>(string name, T content, bool compress = false,
        Type? contentType = null, bool forceCreation = false) where T : class
    {
        if (!IsValidFileName(name))
        {
            _logger.LogError("File name cannot be null or empty.");
            return Result<DropBearFile>.Failure("Invalid file name.");
        }

        name = Path.GetFileNameWithoutExtension(name); // Ensure name validity

        try
        {
            var container = CreateContentContainer(name, content, compress, contentType);
            var dropBearFile = new DropBearFile(name, Environment.UserName, compress);
            dropBearFile.AddContent(container);

            var validationResult = await ValidateFileAsync(dropBearFile).ConfigureAwait(false);
            if (!validationResult.IsValid && !forceCreation)
            {
                _logger.LogWarning($"File validation failed: {validationResult.Errors}");
                return Result<DropBearFile>.Failure("Validation failed.");
            }

            _logger.LogInformation("DropBear file created successfully.");
            return Result<DropBearFile>.Success(dropBearFile);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating DropBear file: {ex.Message}");
            return Result<DropBearFile>.Failure(ex.Message);
        }
    }

    /// <summary>
    ///     Writes a DropBear file asynchronously.
    /// </summary>
    /// <param name="file">The DropBear file to write.</param>
    /// <param name="filePath">The path where the file should be written.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task WriteFileAsync(DropBearFile file, string filePath)
    {
        try
        {
            filePath = SanitizeFilePath(filePath);
            var fullFilePathAndName = ConstructFilePath(file, filePath);

            EnsureDirectoryExists(filePath);

            // Instead of writing directly to a FileStream, use a RecyclableMemoryStream for the initial processing.
            using (var memoryStream = _recyclableMemoryStreamManager.GetStream("FileManager.WriteFileAsync"))
            {
                await SerializeDropBearFileComponents(file, memoryStream).ConfigureAwait(false);

                // After serializing the file components into the memory stream, compute and append the hash.
                memoryStream.Position = 0; // Ensure the stream is at the beginning before computing the hash.
                var fileHash = await ComputeAndAppendHashAsync(memoryStream).ConfigureAwait(false);
                _logger.LogInformation($"Computed file hash: {BitConverter.ToString(fileHash)}");

                // Reset the stream position after computing the hash, before writing to the file.
                memoryStream.Position = 0;

                // Write from the memory stream to the actual file.
                using (var fileStream =
                       new FileStream(fullFilePathAndName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
                }

                _logger.LogInformation("DropBear file written successfully with RecyclableMemoryStream.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error writing DropBear file with RecyclableMemoryStream: {ex.Message}");
        }
    }


    /// <summary>
    ///     Reads a DropBear file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the DropBear file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The read DropBear file.</returns>
    public async Task<Result<DropBearFile>> ReadFileAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate and sanitize the filePath for security reasons.
            filePath = SanitizeFilePath(filePath);

            // Use FileStream to open the file for reading.
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Instead of reading the entire file into a standard MemoryStream,
                // use a RecyclableMemoryStream for the file reading process.
                using (var memoryStream = _recyclableMemoryStreamManager.GetStream("FileManager.ReadFileAsync"))
                {
                    // Copy the FileStream to the RecyclableMemoryStream.
                    await fileStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

                    // After copying, reset the position of the memory stream to start reading from the beginning.
                    memoryStream.Position = 0;

                    // Read components and perform file integrity verification within the memory stream.
                    var readResult = await ReadComponentsAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                    if (!readResult.IsSuccess)
                    {
                        _logger.LogError(readResult.ErrorMessage);
                        return Result<DropBearFile>.Failure(readResult.ErrorMessage);
                    }

                    var verificationResult =
                        await VerifyFileIntegrityAsync(memoryStream, readResult.Components, cancellationToken)
                            .ConfigureAwait(false);
                    if (!verificationResult)
                    {
                        _logger.LogError("File hash verification failed.");
                        return Result<DropBearFile>.Failure("File hash verification failed.");
                    }

                    _logger.LogInformation("DropBear file read successfully with RecyclableMemoryStream.");
                    return DeserializeDropBearFile(readResult.Components, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error reading DropBear file with RecyclableMemoryStream: {ex.Message}");
            return Result<DropBearFile>.Failure($"Error reading DropBear file: {ex.Message}");
        }
    }


    /// <summary>
    ///     Deletes a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    public async Task DeleteFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath))
                    .ConfigureAwait(false); // Consider Task.Run for minimal overhead async wrap
                _logger.LogInformation($"File deleted successfully: {filePath}");
            }
            else
            {
                _logger.LogWarning($"File not found: {filePath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting file: {filePath}. Exception: {ex.Message}");
        }
    }

    /// <summary>
    ///     Updates a file with new content.
    /// </summary>
    /// <param name="filePath">The path to the file to update.</param>
    /// <param name="newContent">The new content for the file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateFileAsync(string filePath, DropBearFile newContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (directoryPath is null) throw new InvalidOperationException("Invalid file path.");

            // Delete the existing file if it exists
            await DeleteFileAsync(filePath).ConfigureAwait(false); // Use async version for consistency

            // Write the new content to the file
            await WriteFileAsync(newContent, filePath).ConfigureAwait(false); // Directly use filePath for clarity

            _logger.LogInformation($"File updated successfully: {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating file: {filePath}. Exception: {ex.Message}");
        }
    }

    #endregion

    #region Protected and Internal Methods

    #endregion

    #region Private Methods

    #region Container/Component Methods

    private static ContentContainer CreateContentContainer<T>(string name, T content, bool compress, Type? contentType)
        where T : class
    {
        if (contentType != null)
        {
            var serializedContent =
                JsonSerializer.SerializeToString(content).GetBytes(); // Assuming these methods exist and are correct
            return new ContentContainer(name, serializedContent,
                new ContentTypeInfo(contentType.Assembly.FullName!, contentType.Name, contentType.Namespace!),
                compress);
        }

        if (typeof(T) == typeof(byte[]))
            return new ContentContainer(typeof(byte[]), name,
                content as byte[] ?? throw new InvalidOperationException("Content cannot be null or empty."), compress);
        return new ContentContainer<T>(name, content, compress);
    }

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

    #endregion

    #region Validation Methods

    private static bool IsValidFileName(string? name) => !string.IsNullOrWhiteSpace(name);

    private async Task<ValidationResult> ValidateFileAsync(DropBearFile file)
    {
        var validationTasks = new List<Task<ValidationResult>>
        {
            _strategyValidator.ValidateAsync(file)
            // Add other validations as needed
        };

        var validationResults = await Task.WhenAll(validationTasks).ConfigureAwait(false);
        var aggregatedResult = validationResults.Aggregate(ValidationResult.Success(),
            (current, result) => current.Combine(result));

        return aggregatedResult;
    }

    private static string SanitizeFilePath(string filePath) =>
        // Implementation to sanitize and validate the filePath to prevent path traversal or other security issues.
        Path.GetFullPath(filePath);

    private void EnsureDirectoryExists(string filePath)
    {
        if (Directory.Exists(filePath)) return;
        Directory.CreateDirectory(filePath);
        _logger.LogInformation($"Directory created: {filePath}");
    }

    private static string ConstructFilePath(DropBearFile file, string filePath) =>
        // Construct and return the full file path and name based on file metadata and header.
        Path.Combine(filePath, $"{file.Metadata.FileName}.{file.Header?.Signature.Extension}");

    #endregion

    #region Serialization/Deserialization Methods

    /// <summary>
    ///     Deserializes the components of a DropBearFile from byte arrays.
    /// </summary>
    /// <param name="componentData">The byte array components representing the DropBearFile.</param>
    /// <param name="cancellationToken">Optional. A token to monitor for cancellation requests.</param>
    /// <returns>A reconstructed DropBearFile object.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the data is insufficient to reconstruct the DropBearFile or
    ///     specific components are missing.
    /// </exception>
#pragma warning disable IDE0051
    private static DropBearFile DeserializeDropBearFile(IEnumerable<byte[]> componentData,
        CancellationToken cancellationToken = default)
#pragma warning restore IDE0051
    {
        var components = componentData as byte[][] ?? componentData.ToArray();
        if (components.Length < 3) // Minimum expected: Header, FileMetadata, and FileContent
            throw new InvalidOperationException("Insufficient data to reconstruct DropBearFile.");

        // Deserialize components using the generic method
        var header = DeserializeComponent<FileHeader>(components[0], cancellationToken);
        var metadata = DeserializeComponent<FileMetadata>(components[1], cancellationToken);
        var content = components.Length > 2
            ? DeserializeComponent<FileContent>(components[2], cancellationToken)
            : throw new InvalidOperationException("FileContent data is missing.");

        return DropBearFile.Reconstruct(header, metadata, content);
    }

    /// <summary>
    ///     Generic method for deserializing a component from a byte array.
    /// </summary>
    /// <typeparam name="T">The type of the component to deserialize.</typeparam>
    /// <param name="data">The byte array representing the component.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The deserialized component.</returns>
    private static T DeserializeComponent<T>(byte[] data, CancellationToken cancellationToken = default) =>
        // Ensure proper configuration of _options to mitigate deserialization risks.
        MessagePackSerializer.Deserialize<T>(data, Options, cancellationToken);

    private static async Task SerializeDropBearFileComponents(DropBearFile file, Stream fileStream)
    {
        // Serialize and write each component of DropBearFile with length prefix.
        await WriteComponentWithLengthPrefixAsync(fileStream, file.Header).ConfigureAwait(false);
        await WriteComponentWithLengthPrefixAsync(fileStream, file.Metadata).ConfigureAwait(false);
        await WriteComponentWithLengthPrefixAsync(fileStream, file.Content).ConfigureAwait(false);
    }

    private static Result<DropBearFile> DeserializeDropBearFile(IList<byte[]> components,
        CancellationToken cancellationToken)
    {
        try
        {
            // Assume a method exists on DropBearFile that can reconstruct it from its components
            var file = DropBearFile.Reconstruct(new Collection<byte[]>(components), cancellationToken);
            return Result<DropBearFile>.Success(file);
        }
        catch (Exception ex)
        {
            return Result<DropBearFile>.Failure($"Deserialization failed: {ex.Message}");
        }
    }

    #endregion

    #region Hashing Methods

    private static bool VerifyHash(byte[] computedHash, byte[] expectedHash) =>
        computedHash.AsSpan().SequenceEqual(expectedHash.AsSpan());

    /// <summary>
    ///     Computes a SHA256 hash of the contents of a stream.
    /// </summary>
    /// <param name="stream">The stream to compute the hash from.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The computed hash as a byte array.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the stream does not support reading or other errors occur.</exception>
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

    /// <summary>
    ///     Computes and appends a SHA256 hash to the provided stream, then returns the hash.
    ///     Ensures the stream is at the beginning before hashing and is writable.
    /// </summary>
    /// <param name="stream">The stream to hash and append the hash to.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The binary hash as a byte array.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the stream is not writable.</exception>
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

    #endregion

    #region Stream Methods

    /// <summary>
    ///     Reads a hash from the end of the stream.
    /// </summary>
    /// <param name="stream">The stream from which to read the hash.</param>
    /// <param name="hashSize">The size of the hash in bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The hash read from the end of the stream.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the stream is not seekable or is too short for the expected hash
    ///     size.
    /// </exception>
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

    /// <summary>
    ///     Reads an exact number of bytes from a stream into a buffer, respecting the specified cancellation token.
    /// </summary>
    /// <param name="stream">The stream from which to read.</param>
    /// <param name="buffer">The buffer into which the data is to be read.</param>
    /// <param name="cancellationToken">Cancellation token to observe.</param>
    /// <exception cref="InvalidOperationException">Thrown if the stream does not support reading.</exception>
    /// <exception cref="EndOfStreamException">Thrown if the stream ends before the desired amount of data is read.</exception>
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

            if (bytesRead == 0)
                // The stream ended unexpectedly, which might indicate a corrupted or incomplete stream.
                throw new EndOfStreamException(
                    $"The stream ended before all the data could be read. Total bytes read: {totalBytesRead}, Expected: {bytesToRead}.");

            totalBytesRead += bytesRead;
        }
    }

    /// <summary>
    ///     Reads data from a stream that was written with a length prefix.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The data read from the stream.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the stream does not support seeking or if declared data length is
    ///     invalid.
    /// </exception>
    /// <exception cref="EndOfStreamException">Thrown if there are not enough bytes left in the stream for the data.</exception>
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

    /// <summary>
    ///     Writes data to a stream with a length prefix.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="data">The data to write, preceded by its length as a 4-byte integer.</param>
    /// <remarks>
    ///     This method ensures data integrity by prefixing the data with its length, allowing precise reading of the expected
    ///     data amount.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the stream does not support writing.</exception>
    private static async Task WriteWithLengthPrefixAsync(Stream stream, byte[] data)
    {
        if (!stream.CanWrite) throw new InvalidOperationException("Stream must be writable.");

        // Convert the length of the data to a byte array and write it to the stream.
        var lengthPrefix = BitConverter.GetBytes(data.Length);
        await stream.WriteAsync(lengthPrefix).ConfigureAwait(false);
        await stream.WriteAsync(data).ConfigureAwait(false);
    }


    /// <summary>
    ///     Serializes a component with MessagePack and writes it to a stream with a length prefix.
    /// </summary>
    /// <typeparam name="T">The type of the component to serialize.</typeparam>
    /// <param name="fileStream">The stream to which the serialized component will be written.</param>
    /// <param name="component">The component to serialize and write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the stream is not writable.</exception>
    private static async Task WriteComponentWithLengthPrefixAsync<T>(Stream fileStream, T component)
    {
        // Check if the file stream supports writing.
        if (!fileStream.CanWrite)
            throw new InvalidOperationException("The stream must be writable to perform write operations.");

        try
        {
            // Serialize the component using MessagePack with provided options.
            var componentBytes = MessagePackSerializer.Serialize(component, Options);

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

    #endregion

    #region Signature Verification

    /// <summary>
    ///     Verifies if the provided signature matches the expected file signature.
    /// </summary>
    /// <param name="actualSignature">The actual signature to verify.</param>
    /// <returns>true if the actual signature matches the expected signature; otherwise, false.</returns>
    /// <remarks>
    ///     Logs an error and returns false if an unexpected issue occurs during verification.
    /// </remarks>
#pragma warning disable IDE0051
    private bool VerifyFileSignature(byte[] actualSignature)
#pragma warning restore IDE0051
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
            _logger.LogError($"Error verifying file signature: {ex.Message}");
            return false;
        }
    }

    #endregion

    #endregion

    #region Events and Handlers

    #endregion

    #region Overrides

    #endregion

    #region Nested Classes

    #endregion
}
