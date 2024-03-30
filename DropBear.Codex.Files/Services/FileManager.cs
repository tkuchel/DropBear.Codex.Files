using System.Text;
using DropBear.Codex.AppLogger.Interfaces;
using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using DropBear.Codex.Files.PreflightTasks;
using DropBear.Codex.Files.Utils;
using DropBear.Codex.Files.Validation.Strategies;
using DropBear.Codex.Serialization.Enums;
using DropBear.Codex.Serialization.Interfaces;
using DropBear.Codex.Utilities.Hashing.Interfaces;
using DropBear.Codex.Utilities.MessageTemplates;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;

namespace DropBear.Codex.Files.Services;

/// <summary>
///     Service responsible for file management operations such as creation, reading, updating, and deletion of DropBear
///     files.
/// </summary>
public class FileManager : IFileManager
{
    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="FileManager" /> class.
    /// </summary>
    /// <param name="logger">The logger for logging messages.</param>
    /// <param name="strategyValidator">The validator for validating strategies.</param>
    /// <param name="dataSerializer">The data serializer for serializing and deserializing data.</param>
    /// <param name="hasher">The hashing service for hashing data.</param>
    /// <param name="messageTemplateManager">The message template manager for managing message templates.</param>
    public FileManager(IAppLogger<FileManager> logger, IStrategyValidator strategyValidator,
        IDataSerializer dataSerializer, IHashingService hasher, IMessageTemplateManager messageTemplateManager)
    {
        _strategyValidator = strategyValidator;
        _logger = logger;
        _dataSerializer = dataSerializer;
        _hasher = hasher;
        _messageTemplateManager = messageTemplateManager;


        Init();
    }

    private void Init()
    {
        var initializationResult = InitializeFileManager();
        if (initializationResult.IsFailure) _logger.LogError(initializationResult.ErrorMessage);
    }

    #endregion

    #region Field and Property Definitions

    private static readonly IReadOnlyList<byte> ExpectedSignatureBytes = new FileHeader().FileSignature.Signature;
    private readonly IDataSerializer _dataSerializer;
    private readonly IHashingService _hasher;
    private readonly IMessageTemplateManager _messageTemplateManager;
    private readonly IAppLogger<FileManager> _logger;
    private readonly IStrategyValidator _strategyValidator;

    #endregion

    #region Public Methods

    /// <summary>
    ///     Creates a DropBear file asynchronously with the specified author, content, and optional compression settings.
    /// </summary>
    /// <param name="author">The author of the file.</param>
    /// <param name="content">The content of the file.</param>
    /// <param name="compressContent">Indicates whether the file contents should be compressed.</param>
    /// <returns>A task representing the asynchronous operation and containing the created DropBear file.</returns>
    public async Task<Result<DropBearFile>> CreateFileAsync(string author, IFileContent content,
        bool compressContent = false)
    {
        var metaData = new FileMetaData(author, content);
        var compressionSettings = new CompressionSettings(compressContent);
        var createdFile = new DropBearFile(metaData, compressionSettings, content);

        // Aggregate all validation results
        var allValidationResults = new List<ValidationResult>
        {
            await _strategyValidator.ValidateAsync(createdFile.Header).ConfigureAwait(false),
            await _strategyValidator.ValidateAsync(createdFile.MetaData).ConfigureAwait(false),
            await _strategyValidator.ValidateAsync(createdFile.CompressionSettings).ConfigureAwait(false),
            await _strategyValidator.ValidateAsync(createdFile.Content).ConfigureAwait(false),
            await _strategyValidator.ValidateAsync(createdFile).ConfigureAwait(false)
        };

        // Combine all errors from the validation results if any validation failed
        if (allValidationResults.Exists(result => !result.IsValid))
        {
            var allErrors = allValidationResults.SelectMany(result => result.Errors)
                .Select(e => e.ErrorMessage)
                .ToArray();
            var errorMessage = string.Join("; ", allErrors);
            var errorBytes = Encoding.UTF8.GetBytes(errorMessage);

            // Log the validation failure. Make sure to convert byte[] to a string properly.
            _logger.LogError(Encoding.UTF8.GetString(errorBytes));
            return Result<DropBearFile>.Failure(errorMessage);
        }

        // Messagepack serialization checks
        var headerOk = await _dataSerializer.IsMessagePackSerializable<FileHeader>().ConfigureAwait(false);

        if (!headerOk.IsSuccess)
        {
            _logger.LogError("FileHeader is not serializable.");
            return Result<DropBearFile>.Failure("FileHeader is not serializable.");
        }

        var metaDataOk = await _dataSerializer.IsMessagePackSerializable<FileMetaData>().ConfigureAwait(false);

        if (!metaDataOk.IsSuccess)
        {
            _logger.LogError("FileMetaData is not serializable.");
            return Result<DropBearFile>.Failure("FileMetaData is not serializable.");
        }

        var compressionSettingsOk =
            await _dataSerializer.IsMessagePackSerializable<CompressionSettings>().ConfigureAwait(false);

        if (!compressionSettingsOk.IsSuccess)
        {
            _logger.LogError("CompressionSettings is not serializable.");
            return Result<DropBearFile>.Failure("CompressionSettings is not serializable.");
        }

        var contentOk = await _dataSerializer.IsMessagePackSerializable<FileContent>().ConfigureAwait(false);

        if (!contentOk.IsSuccess)
        {
            _logger.LogError("FileContent is not serializable.");
            return Result<DropBearFile>.Failure("FileContent is not serializable.");
        }

        _logger.LogInformation("File created successfully");
        return Result<DropBearFile>.Success(createdFile);
    }

    /// <summary>
    ///     Writes a DropBear file to the specified file path asynchronously.
    /// </summary>
    /// <param name="file">The DropBear file to write.</param>
    /// <param name="filePath">The file path where the file will be written.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<Result> WriteFileAsync(DropBearFile file, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogError("File path cannot be null or whitespace.");
            return Result.Failure("File path cannot be null or whitespace.");
        }

        if (!Path.IsPathFullyQualified(filePath) || !Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            _logger.LogError("File path is invalid or the directory does not exist.");
            return Result.Failure("File path is invalid or the directory does not exist.");
        }

        try
        {
            var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096,
                true);
            await using (fileStream.ConfigureAwait(false))
            {
                // Your existing write operations...
                await WriteComponentsToFileStreamAsync(fileStream, file).ConfigureAwait(false);

                // Now this should succeed, as the stream supports reading.
                await AppendVerificationHashAsync(fileStream).ConfigureAwait(false);

                _logger.LogInformation($"File written and verified successfully at {filePath}");
                return Result.Success();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to write the file at {filePath}");
            return Result.Failure($"An error occurred while writing the file: {ex.Message}");
        }
    }

    /// <summary>
    ///     Reads a DropBear file asynchronously from the specified file path.
    /// </summary>
    /// <param name="filePath">The file path from which to read the file.</param>
    /// <returns>A task representing the asynchronous operation and containing the read DropBear file.</returns>
    public async Task<Result<DropBearFile>> ReadFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogError("File path cannot be null or whitespace.");
            return Result<DropBearFile>.Failure("File path cannot be null or whitespace.");
        }

        if (!File.Exists(filePath))
        {
            _logger.LogError("File does not exist at the specified path.");
            return Result<DropBearFile>.Failure("File does not exist at the specified path.");
        }

        try
        {
            var fileStream =
                new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            await using (fileStream.ConfigureAwait(false))
            {
                // // Read the file signature using LengthPrefixUtils
                // var fileSignatureBytes = LengthPrefixUtils.ReadLengthPrefixedBytes(fileStream);
                //
                // // Verify the file signature
                // if (!IsValidFileSignature(fileSignatureBytes))
                // {
                //     _logger.LogError("Invalid file signature.");
                //     return Result<DropBearFile>.Failure("Invalid file signature.");
                // }

                var fileHeader = await ReadAndDeserializeHeaderAsync(fileStream).ConfigureAwait(false);

                // Verify the file signature within the FileHeader
                if (!IsValidFileSignature(fileHeader.FileSignature.Signature))
                {
                    _logger.LogError("Invalid file signature.");
                    return Result<DropBearFile>.Failure("Invalid file signature.");
                }

                // Read the main appended hash for verification
                var appendedHash = LengthPrefixUtils.ReadLengthPrefixedBytes(fileStream);
                // Assume a method to verify the hash against the content
                if (!await VerifyAppendedHashAsync(fileStream, appendedHash).ConfigureAwait(false))
                {
                    _logger.LogError("File verification failed.");
                    return Result<DropBearFile>.Failure("File verification failed.");
                }

                // Assuming methods to read and deserialize each component
                //var header = await ReadAndDeserializeHeaderAsync(fileStream).ConfigureAwait(false);
                var metaData = await ReadAndDeserializeMetaDataAsync(fileStream).ConfigureAwait(false);
                var compressionSettings =
                    await ReadAndDeserializeCompressionSettingsAsync(fileStream).ConfigureAwait(false);
                var content = await ReadAndDeserializeContentAsync(fileStream, compressionSettings)
                    .ConfigureAwait(false);

                var reconstructedFile = DropBearFile.Reconstruct(fileHeader, metaData, compressionSettings, content);

                _logger.LogInformation($"File read successfully from {filePath}");
                return Result<DropBearFile>.Success(reconstructedFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to read the file from {filePath}");
            return Result<DropBearFile>.Failure("An error occurred while reading the file.");
        }
    }


    /// <summary>
    ///     Deletes the file at the specified file path.
    /// </summary>
    /// <param name="filePath">The file path of the file to delete.</param>
    /// <returns>A result indicating the outcome of the operation.</returns>
    public Result DeleteFile(string filePath)
    {
        // Validate the file path
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogError("File path cannot be null or whitespace.");
            return Result.Failure("File path cannot be null or whitespace.");
        }

        // Check if the file exists
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Attempted to delete a file that does not exist: {filePath}.");
            return Result.Failure("File does not exist.");
        }

        try
        {
            // Delete the file from the file system
            File.Delete(filePath);

            // Log the file deletion operation
            _logger.LogInformation($"File successfully deleted: {filePath}.");

            return Result.Success();
        }
        catch (Exception ex)
        {
            // Log any exception that occurs during the deletion
            _logger.LogError(ex, $"Failed to delete the file: {filePath}.");

            return Result.Failure("An error occurred while trying to delete the file.");
        }
    }

    /// <summary>
    ///     Updates the specified DropBear file asynchronously at the specified file path.
    /// </summary>
    /// <param name="file">The DropBear file to update.</param>
    /// <param name="filePath">The file path where the updated file will be written.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<Result<DropBearFile>> UpdateFileAsync(DropBearFile file, string filePath)
    {
        // Validate the file path
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogError("File path cannot be null or whitespace.");
            return Result<DropBearFile>.Failure("File path cannot be null or whitespace.");
        }

        if (!File.Exists(filePath))
        {
            _logger.LogError($"File does not exist at the specified path: {filePath}.");
            return Result<DropBearFile>.Failure("File does not exist.");
        }

        try
        {
            // Update the last modified timestamp of the file metadata
            file.MetaData.UpdateLastModified();

            var fileStream =
                new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None, 4096, true);
            await using (fileStream.ConfigureAwait(false))
            {
                // Serialize and write each component to the stream using length-prefixed bytes
                await WriteComponentsToFileStreamAsync(fileStream, file).ConfigureAwait(false);

                // Append a verification hash to the end of the file
                await AppendVerificationHashAsync(fileStream).ConfigureAwait(false);

                _logger.LogInformation($"File updated successfully: {filePath}");
                return Result<DropBearFile>.Success(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to update the file: {filePath}");
            return Result<DropBearFile>.Failure("An error occurred while updating the file.");
        }
    }

    #endregion

    #region Protected and Internal Methods

    #endregion

    #region Private Methods

    // Adjust IsValidFileSignature to accept the exact signature byte array
    private static bool IsValidFileSignature(IEnumerable<byte> signatureBytes)
    {
        // Convert ExpectedSignatureBytes to IEnumerable<byte> for sequence comparison
        // This assumes ExpectedSignatureBytes is originally a byte[]
        IEnumerable<byte> expectedSignature = ExpectedSignatureBytes;

        return signatureBytes.SequenceEqual(expectedSignature);
    }


    private async Task<bool> VerifyAppendedHashAsync(Stream fileStream, IReadOnlyCollection<byte> appendedHash)
    {
        // Reset the position to the start of the file stream for reading
        fileStream.Position = 0;

        // Exclude the appended hash from the content to be hashed
        var contentLength = (int)fileStream.Length - appendedHash.Count;
        var contentBytes = new byte[contentLength];

        // Read the content bytes excluding the appended hash
        var totalBytesRead = 0;
        int bytesRead;
        while (totalBytesRead < contentLength && (bytesRead = await fileStream
                   .ReadAsync(contentBytes.AsMemory(totalBytesRead, contentLength - totalBytesRead))
                   .ConfigureAwait(false)) > 0) totalBytesRead += bytesRead;

        // Ensure all expected bytes were read
        if (totalBytesRead != contentLength)
            throw new IOException(
                $"Failed to read the entire content. Expected {contentLength} bytes, read {totalBytesRead} bytes.");

        // Compute the hash of the read content bytes
        var computedHash = ComputeHash(contentBytes);

        // Compare the computed hash with the appended hash
        return computedHash.SequenceEqual(appendedHash);
    }

    private byte[] ComputeHash(byte[] contentBytes)
    {
        var result = _hasher.EncodeToBase64Hash(contentBytes);
        return result.IsSuccess ? Encoding.UTF8.GetBytes(result.Value) : Array.Empty<byte>();
    }

    private async Task<FileHeader> ReadAndDeserializeHeaderAsync(Stream fileStream)
    {
        // Ensure the stream's position is at the beginning or at the correct offset.
        fileStream.Position = 0;

        var headerBytes = LengthPrefixUtils.ReadLengthPrefixedBytes(fileStream);

        // Optional: Log or inspect headerBytes to verify correctness before deserialization.
    
        var header = await _dataSerializer
            .DeserializeMessagePackAsync<FileHeader>(headerBytes, CompressionOption.Compressed).ConfigureAwait(false);
        
        if (header.IsFailure)
        {
            throw new InvalidOperationException("Failed to deserialize FileHeader.");
        }
    
        return header.Value;
    }


    private async Task<FileMetaData> ReadAndDeserializeMetaDataAsync(Stream fileStream)
    {
        var metaDataBytes = LengthPrefixUtils.ReadLengthPrefixedBytes(fileStream);
        var metaData = await _dataSerializer
            .DeserializeMessagePackAsync<FileMetaData>(metaDataBytes, CompressionOption.Compressed)
            .ConfigureAwait(false);
        if (metaData.IsFailure) throw new InvalidOperationException("Failed to deserialize FileMetaData.");
        return metaData.Value;
    }

    private async Task<CompressionSettings> ReadAndDeserializeCompressionSettingsAsync(Stream fileStream)
    {
        var compressionSettingsBytes = LengthPrefixUtils.ReadLengthPrefixedBytes(fileStream);
        var compressionSettings = await _dataSerializer
            .DeserializeMessagePackAsync<CompressionSettings>(compressionSettingsBytes, CompressionOption.Compressed)
            .ConfigureAwait(false);
        if (compressionSettings.IsFailure)
            throw new InvalidOperationException("Failed to deserialize CompressionSettings.");
        return compressionSettings.Value;
    }

    private async Task<IFileContent> ReadAndDeserializeContentAsync(Stream fileStream,
        ICompressionSettings compressionSettings)
    {
        var contentBytes = LengthPrefixUtils.ReadLengthPrefixedBytes(fileStream);
        var compressionOption =
            compressionSettings.IsCompressed ? CompressionOption.Compressed : CompressionOption.None;
        var fileContentResult = await _dataSerializer
            .DeserializeMessagePackAsync<FileContent>(contentBytes, compressionOption).ConfigureAwait(false);

        if (fileContentResult.IsFailure)
            throw new InvalidOperationException(
                $"Failed to {(compressionSettings.IsCompressed ? "decompress" : "deserialize")} FileContent: {fileContentResult.ErrorMessage}");

        return fileContentResult.Value;
    }

    /// <summary>
    ///     Serializes and writes the components of a DropBearFile to the provided file stream.
    /// </summary>
    /// <param name="fileStream">The stream to which components will be written.</param>
    /// <param name="file">The DropBearFile whose components are to be serialized and written.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if any component fails to serialize.</exception>
    private async Task WriteComponentsToFileStreamAsync(Stream fileStream, DropBearFile file)
    {
        try
        {
            var headerBytes = await _dataSerializer
                .SerializeMessagePackAsync(file.Header, CompressionOption.Compressed).ConfigureAwait(false);
            if (!headerBytes.IsSuccess) throw new InvalidOperationException("Failed to serialize the file header.");
            LengthPrefixUtils.WriteLengthPrefixedBytes(fileStream, headerBytes.Value);

            var metaDataBytes = await _dataSerializer
                .SerializeMessagePackAsync(file.MetaData, CompressionOption.Compressed).ConfigureAwait(false);
            if (!metaDataBytes.IsSuccess) throw new InvalidOperationException("Failed to serialize the file metadata.");
            LengthPrefixUtils.WriteLengthPrefixedBytes(fileStream, metaDataBytes.Value);

            var compressionSettingsBytes = await _dataSerializer
                .SerializeMessagePackAsync(file.CompressionSettings, CompressionOption.Compressed)
                .ConfigureAwait(false);
            if (!compressionSettingsBytes.IsSuccess)
                throw new InvalidOperationException("Failed to serialize the compression settings.");
            LengthPrefixUtils.WriteLengthPrefixedBytes(fileStream, compressionSettingsBytes.Value);

            var contentBytes = await _dataSerializer
                .SerializeMessagePackAsync(file.Content,
                    file.CompressionSettings.IsCompressed ? CompressionOption.Compressed : CompressionOption.None)
                .ConfigureAwait(false);
            if (!contentBytes.IsSuccess) throw new InvalidOperationException("Failed to serialize the file content.");
            LengthPrefixUtils.WriteLengthPrefixedBytes(fileStream, contentBytes.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write the components to the file stream.");
            throw;
        }
    }

    private async Task AppendVerificationHashAsync(Stream fileStream)
    {
        if (!fileStream.CanRead) throw new NotSupportedException("The provided stream does not support reading.");
        if (!fileStream.CanSeek) throw new NotSupportedException("The provided stream does not support seeking.");

        try
        {
            // Ensure the stream position is reset to the beginning for reading
            fileStream.Position = 0;

            // Initialize a buffer to hold the file content
            var fileContentBytes = new byte[fileStream.Length];

            // Read the entire file content into the buffer
            var bytesRead = await fileStream.ReadAsync(fileContentBytes)
                .ConfigureAwait(false);
            if (bytesRead != fileStream.Length) throw new IOException("Could not read the full content of the stream.");

            // Compute the hash of the file content
            var hashResult = _hasher.EncodeToBase64Hash(fileContentBytes);
            if (!hashResult.IsSuccess) throw new InvalidOperationException("Failed to compute the file content hash.");

            // Write the computed hash to the end of the stream
            // This assumes the stream supports writing. Consider checking fileStream.CanWrite if necessary.
            var hashBytes = Encoding.UTF8.GetBytes(hashResult.Value);
            await fileStream.WriteAsync(hashBytes).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append the verification hash to the file stream.");
            throw;
        }
    }

    private Result InitializeFileManager()
    {
        try
        {
            _logger.LogInformation("Initializing FileManager.");

            // Register validation strategies
            _logger.LogInformation("Registering validation strategies.");
            _strategyValidator.RegisterStrategy(new CompressionSettingsValidationStrategy());
            _strategyValidator.RegisterStrategy(new FileContentValidationStrategy());
            _strategyValidator.RegisterStrategy(new FileHeaderValidationStrategy());
            _strategyValidator.RegisterStrategy(new FileMetaDataValidationStrategy());
            _strategyValidator.RegisterStrategy(new DropBearFileValidationStrategy());
            _logger.LogInformation("Validation strategies registered successfully.");

            // Register message templates
            _logger.LogInformation("Registering message templates.");
            _messageTemplateManager.RegisterTemplates(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Test", "Test" }
            });
            _logger.LogInformation("Message templates registered successfully.");

            // Perform type checking logic here
            _logger.LogInformation("Checking type compatibility.");
            var typesToCheck = new List<Type>
            {
                typeof(ContentContainer),
                typeof(ContentTypeInfo),
                typeof(FileSignature),
                typeof(CompressionSettings),
                typeof(FileContent),
                typeof(FileHeader),
                typeof(FileMetaData),
                typeof(DropBearFile)
                // typeof(IContentContainer),
                // typeof(ICompressionSettings),
                // typeof(IFileContent),
                // typeof(IFileHeader),
                // typeof(IFileMetaData),
            };
            var results = MessagePackCompatibilityAggregator.CheckTypes(typesToCheck);
            _logger.LogInformation("Type compatibility check completed.");

            if (results.FailedTypes.Count is not 0)
            {
                foreach (var (type, reason) in results.FailedTypes)
                    _logger.LogError($"Type {type} failed compatibility check: {reason}");

                return Result.Failure($"Type compatibility check failed with {results.FailedTypes.Count} failures.");
            }

            _logger.LogInformation("FileManager initialized successfully.");
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    #endregion

    #region Events and Handlers

    #endregion

    #region Overrides

    #endregion

    #region Nested Classes

    #endregion
}
