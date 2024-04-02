using System.Security.Cryptography;
using DropBear.Codex.AppLogger.Interfaces;
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
using ServiceStack.Text;
using static System.BitConverter;

namespace DropBear.Codex.Files.Services;

/// <summary>
///     Service for managing DropBear files.
/// </summary>
public class FileManager : IFileManager
{
    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the FileManager class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="strategyValidator">The strategy validator instance.</param>
    /// <param name="messageTemplateManager">The message template manager instance.</param>
    public FileManager(IAppLogger<FileManager> logger, IStrategyValidator strategyValidator, IMessageTemplateManager messageTemplateManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _strategyValidator = strategyValidator ?? throw new ArgumentNullException(nameof(strategyValidator));
        _messageTemplateManager = messageTemplateManager ?? throw new ArgumentNullException(nameof(messageTemplateManager));
        
        Initialize(); // Call the synchronous initialize method
    }

    private void Initialize()
    {
        _logger.LogInformation("Initializing FileManager...");

        RegisterValidationStrategies();
        RegisterMessageTemplates();

        if (CheckMessagePackCompatibility().IsValid)
        {
            _logger.LogInformation("MessagePack compatibility check passed.");
        }
        else
        {
            _logger.LogWarning("MessagePack compatibility check failed.");
        }

        _logger.LogInformation("FileManager initialized.");
    }
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
    public async Task<DropBearFile?> CreateFileAsync<T>(string name, T content, bool compress = false,
        Type? contentType = null, bool forceCreation = false) where T : class
    {
        try
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogError("File name cannot be null or empty.");
                return null;
            }

            name = Path.GetFileNameWithoutExtension(name); // Strip the file extension from name if it exists

            ContentContainer? container;
            // Handling based on contentType being provided or not
            if (contentType != null)
            {
                var serializedContent = JsonSerializer.SerializeToString(content).GetBytes();
                container = new ContentContainer(name, serializedContent,
                    new ContentTypeInfo(contentType.Assembly.FullName!, contentType.Name, contentType.Namespace!),
                    compress);
            }
            else
            {
                container = typeof(T) == typeof(byte[])
                    ? new ContentContainer(typeof(byte[]), name,
                        content as byte[] ?? throw new InvalidOperationException("Content cannot be null or empty."),
                        compress)
                    : new ContentContainer<T>(name, content, compress);
            }

            var dropBearFile =
                new DropBearFile(name, Environment.UserName, compress);
            dropBearFile.AddContent(container);

            // Perform validation
            var validationTasks = new List<Task<ValidationResult>>
            {
                _strategyValidator.ValidateAsync(dropBearFile)
                // Add other validations as needed
            };

            var validationResults = await Task.WhenAll(validationTasks).ConfigureAwait(false);
            var aggregatedResult = validationResults.Aggregate(ValidationResult.Success(),
                (current, result) => current.Combine(result));

            if (!aggregatedResult.IsValid)
            {
                var errors = string.Join("; ",
                    aggregatedResult.Errors.Select(err => $"{err.Parameter}: {err.ErrorMessage}"));
                _logger.LogWarning($"File validation failed: {errors}");

                if (!forceCreation) // Return null if forceCreation is false and validation failed
                    return null;
            }

            _logger.LogInformation("DropBear file created successfully.");
            return dropBearFile; // Return the file regardless of validation if forceCreation is true
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating DropBear file: {ex.Message}");
            return null;
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
        // Sanitize and resolve the full file path and name
        filePath = Path.GetFullPath(filePath);
        var fullFilePathAndName =
            Path.Combine(filePath, $"{file.Metadata.FileName}.{file.Header?.Signature.Extension}");

        // Ensure the directory exists or create it
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
            _logger.LogInformation($"Directory created: {filePath}");
        }

        // Use FileMode.Create to overwrite any existing file or create a new one
        try
        {
            var fileStream =
                new FileStream(fullFilePathAndName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            await using (fileStream.ConfigureAwait(false))
            {
                // Serialize and write each component of DropBearFile with length prefix
                await WriteComponentWithLengthPrefixAsync(fileStream, file.Header).ConfigureAwait(false);
                await WriteComponentWithLengthPrefixAsync(fileStream, file.Metadata).ConfigureAwait(false);
                await WriteComponentWithLengthPrefixAsync(fileStream, file.Content).ConfigureAwait(false);

                // Calculate and append the verification hash
                var fileHash = await ComputeAndAppendHashAsync(fileStream).ConfigureAwait(false);
                _logger.LogInformation(
                    $"DropBear file written and verified with hash: {BitConverter.ToString(fileHash)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error writing DropBear file: {ex.Message}");
        }
    }

    /// <summary>
    ///     Reads a DropBear file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the DropBear file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The read DropBear file.</returns>
    public async Task<DropBearFile?> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using (fileStream.ConfigureAwait(false))
            {
                var componentDataWithLpe = new List<byte[]>(); // List to hold bytes of all components, including LPE
                var componentDataWithoutLpe = new List<byte[]>(); // List to hold bytes of all components, excluding LPE

                FileHeader? header = null;
                while (fileStream.Position < fileStream.Length - HashSize)
                {
                    // Before reading the component, capture the position to calculate LPE size after.
                    var startPosition = fileStream.Position;

                    var component = await ReadWithLengthPrefixAsync(fileStream, cancellationToken)
                        .ConfigureAwait(false);

                    // Check if this is the first component to be read
                    if (componentDataWithLpe.Count is 0 && componentDataWithoutLpe.Count is 0)
                    {
                        // Deserialize the header and validate the signature
                        header = MessagePackSerializer.Deserialize<FileHeader>(component,
                            cancellationToken: cancellationToken);
                        if (!VerifyFileSignature(header.Signature.Signature))
                        {
                            _logger.LogError("File signature verification failed.");
                            return null;
                        }

                        // Discard the header that was deserialized
                        _ = header;
                    }

                    // Add component bytes to the without LPE list
                    componentDataWithoutLpe.Add(component); // Add component bytes to the list

                    // After reading, calculate the size of LPE based on the change in position.
                    var endPosition = fileStream.Position;
                    var lpeSize =
                        (int)(endPosition - startPosition - component.Length); // Usually, this should be sizeof(int)

                    // Recreate the component byte array including the LPE bytes
                    var lpeBytes = GetBytes(component.Length);

                    // Add the LPE bytes and component bytes to the with LPE list
                    componentDataWithLpe.Add(lpeBytes.Concat(component)
                        .ToArray()); // Combine LPE bytes with component bytes
                }

                // Compute hash excluding the verification hash at the end
                var totalContentBytesForHash = componentDataWithLpe.SelectMany(b => b).ToArray();
                var computedHash = await ComputeHashAsync(new MemoryStream(totalContentBytesForHash), cancellationToken)
                    .ConfigureAwait(false);

                // Read and verify the hash at the end of the file
                var expectedHash = await ReadHashFromEndAsync(fileStream, HashSize, cancellationToken)
                    .ConfigureAwait(false);

                // Final null checks before proceeding
                ArgumentNullException.ThrowIfNull(header);
                ArgumentNullException.ThrowIfNull(componentDataWithoutLpe);
                ArgumentNullException.ThrowIfNull(computedHash);
                ArgumentNullException.ThrowIfNull(expectedHash);

                if (VerifyHash(computedHash, expectedHash))
                    return DeserializeDropBearFile(componentDataWithoutLpe, cancellationToken);


                _logger.LogError("File hash verification failed.");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error reading DropBear file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Deletes a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    public void DeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
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
            // You might want to rethrow the exception or handle it based on your application's needs
        }
    }

    /// <summary>
    ///     Updates a file with new content.
    /// </summary>
    /// <param name="filePath">The path to the file to update.</param>
    /// <param name="newContent">The new content for the file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateFile(string filePath, DropBearFile newContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Delete the existing file if it exists
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation($"Existing file deleted: {filePath}");
            }

            // Write the new content to the file
            await WriteFileAsync(newContent, filePath).ConfigureAwait(false);

            _logger.LogInformation($"File updated successfully: {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating file: {filePath}. Exception: {ex.Message}");
            // Handle or rethrow the exception as necessary
        }
    }

    #endregion

    #region Private Methods

    private bool VerifyFileSignature(byte[] actualSignature)
    {
        try
        {
            var expectedSignature =
                new FileSignature().Signature; // Assuming a default constructor sets the expected signature
            //Console.WriteLine(BitConverter.ToString(actualSignature));
            //Console.WriteLine(BitConverter.ToString(expectedSignature));

            return actualSignature.AsSpan().SequenceEqual(expectedSignature.AsSpan());
        }
        catch (Exception ex)
        {
            // Log the error or handle it as necessary
            _logger.LogError($"Error verifying file signature: {ex.Message}");
            return false;
        }
    }

    private static DropBearFile DeserializeDropBearFile(IEnumerable<byte[]> componentData,
        CancellationToken cancellationToken = default)
    {
        var listComponentBytes = componentData.ToList();
        if (listComponentBytes.Count < 3) // Minimum expected: Header, FileMetadata and FileContent
            throw new InvalidOperationException("Insufficient data to reconstruct DropBearFile.");

        var header =
            MessagePackSerializer.Deserialize<FileHeader>(listComponentBytes[0], cancellationToken: cancellationToken);

        // Deserialize FileMetadata
        var metadata =
            MessagePackSerializer.Deserialize<FileMetadata>(listComponentBytes[1], _options, cancellationToken);

        // Deserialize FileContent
        // Ensure there is a third component; otherwise, handle as an error or a special case.
        var content = listComponentBytes.Count > 1
            ? MessagePackSerializer.Deserialize<FileContent>(listComponentBytes[2], _options, cancellationToken)
            : throw new InvalidOperationException("FileContent data is missing.");

        // Reconstruct the DropBearFile object with the deserialized components
        var dropBearFile = DropBearFile.Reconstruct(header, metadata, content);
        return dropBearFile;
    }

    private static bool VerifyHash(byte[] computedHash, byte[] expectedHash) =>
        computedHash.AsSpan().SequenceEqual(expectedHash.AsSpan());

    private void RegisterValidationStrategies()
    {
        // Register validation strategies with the StrategyValidator service
        _logger.LogInformation("Registering validation strategies.");
        _strategyValidator.RegisterStrategy(new FileContentValidationStrategy());
        _strategyValidator.RegisterStrategy(new FileHeaderValidationStrategy());
        _strategyValidator.RegisterStrategy(new FileMetaDataValidationStrategy());
        _strategyValidator.RegisterStrategy(new DropBearFileValidationStrategy());
        _logger.LogInformation("Validation strategies registered successfully.");
    }

    private void RegisterMessageTemplates()
    {
        // Register message templates with the MessageTemplateManager service
        _logger.LogInformation("Registering message templates.");
        _messageTemplateManager.RegisterTemplates(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "TestTemplateId", "Test template id: {0}" }
        });
        _logger.LogInformation("Message templates registered successfully.");
    }

    private ValidationResult CheckMessagePackCompatibility()
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

        if (results.FailedTypes.Count is 0) return ValidationResult.Success();

        var validationResult = ValidationResult.Success();
        foreach (var (type, reason) in results.FailedTypes)
        {
            validationResult.AddError(type, reason);
            _logger.LogError($"Type {type} failed compatibility check: {reason}");
        }

        return validationResult;
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
            var componentBytes = MessagePackSerializer.Serialize(component, _options);

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


    /// <summary>
    ///     Computes a SHA256 hash of the contents of a stream.
    /// </summary>
    /// <param name="stream">The stream to compute the hash from.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The computed hash as a byte array.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the stream does not support reading.</exception>
    private static async Task<byte[]> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (!stream.CanRead) throw new InvalidOperationException("Stream must be readable to compute the hash.");

        try
        {
            using var hasher = SHA256.Create();
            var hash = await hasher.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
            return hash;
        }
        catch (Exception ex)
        {
            // This captures any exceptions that might occur during the hash computation,
            // such as issues reading the stream. You might want to log this exception or handle it based on your application's needs.
            throw new InvalidOperationException($"An error occurred while computing the hash: {ex.Message}", ex);
        }
    }


    /// <summary>
    ///     Writes data to a stream with a length prefix.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="data">The data to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the stream is not writable.</exception>
    private static async Task WriteWithLengthPrefixAsync(Stream stream, byte[] data)
    {
        if (!stream.CanWrite) throw new InvalidOperationException("Stream must be writable.");

        var lengthPrefix = GetBytes(data.Length);
        await stream.WriteAsync(lengthPrefix).ConfigureAwait(false);
        await stream.WriteAsync(data).ConfigureAwait(false);
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

        var dataLength = ToInt32(lengthPrefix, 0);
        if (dataLength < 0 || stream.Length - stream.Position < dataLength)
            throw new InvalidOperationException(
                "Invalid data length or not enough bytes in the stream for the declared data length.");

        var data = new byte[dataLength];
        await ReadExactAsync(stream, data, cancellationToken).ConfigureAwait(false);

        return data;
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

    #endregion

    #region Fields and Properties

    private const int HashSize = 32; // 256 bits

    private static readonly MessagePackSerializerOptions _options = MessagePackSerializerOptions.Standard
        .WithCompression(MessagePackCompression.Lz4BlockArray)
        .WithSecurity(MessagePackSecurity.UntrustedData);

    private readonly IAppLogger<FileManager> _logger;
    private readonly IMessageTemplateManager _messageTemplateManager;
    private readonly IStrategyValidator _strategyValidator;

    #endregion
}
