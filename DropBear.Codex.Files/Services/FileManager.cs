using System.Security;
using System.Security.Cryptography;
using System.Text;
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

public class FileManager : IFileManager
{
    private const int HashSize = 32; // 256 bits
    private readonly IAppLogger<FileManager> _logger;
    private readonly IMessageTemplateManager _messageTemplateManager;
    private readonly IStrategyValidator _strategyValidator;

    public FileManager(IAppLogger<FileManager> logger, IStrategyValidator strategyValidator,
        IMessageTemplateManager messageTemplateManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _strategyValidator = strategyValidator ?? throw new ArgumentNullException(nameof(strategyValidator));
        _messageTemplateManager =
            messageTemplateManager ?? throw new ArgumentNullException(nameof(messageTemplateManager));

        // Trigger initialization
        _ = InitializeAsync();
    }


    public async Task WriteFileAsync(DropBearFile file, string filePath)
    {
        var options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray)
            .WithSecurity(MessagePackSecurity.UntrustedData);

        // Sanitize the file path
        filePath = Path.GetFullPath(filePath);
        var fullFilePathAndName = Path.Combine(filePath, $"{file.Metadata.FileName}.{file.Header.Signature.Extension}");

        // Validate the path exists or create it
        if (!Directory.Exists(filePath))
        {
            _logger.LogError("File path does not exist.");
            Directory.CreateDirectory(filePath);
            _logger.LogInformation($"Created directory: {filePath}");
        }

        try
        {
            // Attempt to create or write to the file
            var stream = File.Create(fullFilePathAndName);
            await using (stream.ConfigureAwait(false))
            {
                // File operation
            }

            // If the file was created successfully, delete it to avoid overwriting it
            DeleteFile(fullFilePathAndName);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Unauthorized to write to the file: {filePath}. Error: {ex.Message}");
            // Handle lack of access rights (e.g., log, notify user, retry with different path)
        }
        catch (SecurityException ex)
        {
            _logger.LogError($"Security error when accessing the file: {filePath}. Error: {ex.Message}");
            // Handle security exceptions
        }
        // Catch other specific exceptions as necessary
        catch (Exception ex)
        {
            _logger.LogError($"Error accessing the filepath to write the DropBear file: {ex.Message}");
        }

        try
        {
            var fileStream = new FileStream(fullFilePathAndName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            await using (fileStream.ConfigureAwait(false))
            {
                var headerBytes = MessagePackSerializer.Serialize(file.Header, options);
                await WriteWithLengthPrefixAsync(fileStream, headerBytes).ConfigureAwait(false);

                var metadataBytes = MessagePackSerializer.Serialize(file.Metadata, options);
                await WriteWithLengthPrefixAsync(fileStream, metadataBytes).ConfigureAwait(false);

                var contentBytes = MessagePackSerializer.Serialize(file.Content, options);
                await WriteWithLengthPrefixAsync(fileStream, contentBytes).ConfigureAwait(false);

                // After writing all components, calculate and append the hash
                fileStream.Position = 0; // Reset the stream position to read its content for hashing
                var fileHash = await ComputeAndAppendHashAsync(fileStream).ConfigureAwait(false);

                _logger.LogInformation($"DropBear file written and verified with hash: {fileHash}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error writing DropBear file: {ex.Message}");
        }
    }

    public async Task<DropBearFile?> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using (fileStream.ConfigureAwait(false))
            {
                // Read the FileHeader first to get and verify the signature
                var headerBytes = await ReadWithLengthPrefixAsync(fileStream, cancellationToken).ConfigureAwait(false);
                var header =
                    MessagePackSerializer.Deserialize<FileHeader>(headerBytes, cancellationToken: cancellationToken);

                // Verify the FileSignature within the FileHeader
                if (!VerifyFileSignature(header.Signature.Signature))
                {
                    _logger.LogError("File signature verification failed.");
                    return null;
                }

                // Continue reading the rest of the file (metadata, content, etc.)
                var componentData = new List<byte[]> { headerBytes }; // Include the header bytes already read
                while (fileStream.Position < fileStream.Length - HashSize)
                {
                    long remaining = fileStream.Length - fileStream.Position - HashSize;
                    _logger.LogInformation($"Reading component, stream position: {fileStream.Position}, bytes remaining before hash: {remaining}");

                    var component = await ReadWithLengthPrefixAsync(fileStream, cancellationToken).ConfigureAwait(false);
                    componentData.Add(component);

                    _logger.LogInformation($"Component length: {component.Length}, new stream position: {fileStream.Position}");
                }


                // Compute hash of read content (excluding the appended hash) for verification
                var contentWithoutHash = componentData.SelectMany(a => a).ToArray();
                var computedHash =
                    await ComputeHashAsync(
                        new MemoryStream(contentWithoutHash, 0, contentWithoutHash.Length - HashSize),
                        cancellationToken).ConfigureAwait(false);

                // Read and verify the hash at the end
                var expectedHash = await ReadHashFromEndAsync(fileStream, HashSize, cancellationToken)
                    .ConfigureAwait(false);

                if (VerifyHash(computedHash, expectedHash))
                    return DeserializeDropBearFile(header, componentData.Skip(1)
                        .ToList(), cancellationToken); // Skip the header as it's already processed

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

    private Task InitializeAsync()
    {
        _logger.LogInformation("Initializing FileManager...");

        RegisterValidationStrategies();
        RegisterMessageTemplates();

        if (CheckMessagePackCompatibility().IsValid)
            _logger.LogInformation("MessagePack compatibility check passed.");
        else
            _logger.LogWarning("MessagePack compatibility check failed.");

        _logger.LogInformation("FileManager initialized.");

        return Task.CompletedTask;
    }

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

    private static DropBearFile DeserializeDropBearFile(FileHeader header, List<byte[]> componentData,
        CancellationToken cancellationToken = default)
    {
        if (componentData.Count < 2) // Minimum expected: FileMetadata and FileContent
            throw new InvalidOperationException("Insufficient data to reconstruct DropBearFile.");

        // Assuming the first element (already skipped in the caller) is FileHeader,
        // the second element is FileMetadata, and the third is FileContent.
        var options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        // Deserialize FileMetadata
        var metadata = MessagePackSerializer.Deserialize<FileMetadata>(componentData[0], options, cancellationToken);

        // Deserialize FileContent
        // Ensure there is a third component; otherwise, handle as an error or a special case.
        var content = componentData.Count > 1
            ? MessagePackSerializer.Deserialize<FileContent>(componentData[1], options, cancellationToken)
            : throw new InvalidOperationException("FileContent data is missing.");

        // Reconstruct the DropBearFile object with the deserialized components
        var dropBearFile = new DropBearFile
        {
            Header = header, Metadata = metadata, Content = content
            // Any additional initialization as necessary
        };

        return dropBearFile;
    }

    private static bool VerifyHash(byte[] computedHash, byte[] expectedHash)
    {
        return computedHash.SequenceEqual(expectedHash);
    }


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

    private static async Task WriteWithLengthPrefixAsync(Stream stream, byte[] data)
    {
        var lengthPrefix = GetBytes(data.Length);
        await stream.WriteAsync(lengthPrefix).ConfigureAwait(false);
        await stream.WriteAsync(data).ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadWithLengthPrefixAsync(Stream stream,
        CancellationToken cancellationToken = default)
    {
        var lengthPrefix = new byte[sizeof(int)];
        await ReadExactAsync(stream, lengthPrefix, cancellationToken).ConfigureAwait(false);

        var dataLength = ToInt32(lengthPrefix, 0);
        var data = new byte[dataLength];
        await ReadExactAsync(stream, data, cancellationToken).ConfigureAwait(false);

        return data;
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int totalBytesRead = 0;
        int bytesToRead = buffer.Length;

        while (totalBytesRead < bytesToRead)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, bytesToRead - totalBytesRead), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new EndOfStreamException($"The stream ended before all the data could be read. Total bytes read: {totalBytesRead}, Expected: {bytesToRead}.");
            }
            totalBytesRead += bytesRead;
        }
    }


    private static async Task<byte[]> ReadHashFromEndAsync(Stream stream, int hashSize,
        CancellationToken cancellationToken = default)
    {
        if (stream.Length < hashSize) throw new InvalidOperationException("Stream is shorter than expected hash size.");

        // Seek to the start of the hash
        stream.Seek(-hashSize, SeekOrigin.End);

        var hash = new byte[hashSize];
        await ReadExactAsync(stream, hash, cancellationToken).ConfigureAwait(false);

        return hash;
    }

    private async Task<string> ComputeAndAppendHashAsync(Stream stream,
        CancellationToken cancellationToken = default)
    {
        // Ensure the stream is at the beginning for hashing
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }
        else
        {
            _logger.LogError("Stream cannot seek to beginning for hash computation.");
            throw new InvalidOperationException("Stream cannot seek to beginning.");
        }

        using var hasher = SHA256.Create();
        var hashBytes = await hasher.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "", StringComparison.Ordinal).ToUpperInvariant();

        // Go to the end of the stream to append the hash
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.End);
        }
        else
        {
            _logger.LogError("Stream cannot seek to end for appending hash.");
            throw new InvalidOperationException("Stream cannot seek to end.");
        }

        var hashStringBytes = Encoding.UTF8.GetBytes(hashString);
        await stream.WriteAsync(hashStringBytes, cancellationToken).ConfigureAwait(false);

        return hashString;
    }
    private static async Task<byte[]> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // Reset the position of the stream to the beginning.
        stream.Seek(0, SeekOrigin.Begin);
        using var hasher = SHA256.Create();
        // Compute the hash on the stream directly.
        var hash = await hasher.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return hash; // Return the binary hash
    }

}
