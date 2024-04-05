using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Models.ContentContainers;
using DropBear.Codex.Files.Utils;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ZLogger;
using ILoggerFactory = DropBear.Codex.AppLogger.Interfaces.ILoggerFactory;

namespace DropBear.Codex.Files.Factory.Implementations;

public sealed class FileCreator : IFileCreator, IDisposable
{
    private static RecyclableMemoryStreamManager? s_streamManager;
    private readonly ILogger<FileCreator> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IStrategyValidator _strategyValidator;
    private bool _disposed;
    private bool _useCompression;

    public FileCreator(RecyclableMemoryStreamManager? streamManager, ILoggerFactory loggerFactory,
        IStrategyValidator strategyValidator)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        s_streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        _strategyValidator = strategyValidator ?? throw new ArgumentNullException(nameof(strategyValidator));

        // Create a logger instance for MyClass
        _logger = _loggerFactory.CreateLogger<FileCreator>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IFileCreator WithCompression(bool compress)
    {
        _useCompression = compress;
        return this;
    }

    public async Task<Result<DropBearFile>> CreateAsync<T>(string name, T content, bool compress = false,
        bool forceCreation = false) where T : class
    {
        // Validate the name
        if (string.IsNullOrWhiteSpace(name))
            return Result<DropBearFile>.Failure("Name cannot be null or empty");

        // Remove file extension if present
        name = Path.GetFileNameWithoutExtension(name);

        // Create the container, validate and create the DropBearFile
        try
        {
            // Create a content container based off the name,content,compression and type
            var contentContainer = CreateContentContainer(name, content, compress, typeof(T));

            // Validate the content container
            if (contentContainer is null)
            {
                _logger.ZLogError($"Error creating content container for {name}");
                return Result<DropBearFile>.Failure($"Error creating content container for {name}");
            }

            // Create the DropBearFile
            var dropBearFile = new DropBearFile(name, Environment.UserName, _useCompression);

            // Add the content container to the DropBearFile
            dropBearFile.AddContent(contentContainer);

            var validationResult = await ValidateFileAsync(dropBearFile).ConfigureAwait(false);
            if (validationResult.IsValid || forceCreation) return Result<DropBearFile>.Success(dropBearFile);
            
            _logger.ZLogWarning($"File validation failed: {validationResult.Errors}");
            return Result<DropBearFile>.Failure("Validation failed.");

        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"Error creating file {name}");
            return Result<DropBearFile>.Failure($"Error creating file {name}");
        }
    }

    private static IContentContainer? CreateContentContainer<T>(string name, T content, bool compress, Type type)
        where T : class =>
        // switch based on the type of content
        type switch
        {
            { } t when t == typeof(string) => new StringContentContainer(s_streamManager, name, content as string,
                compress),
            { } t when t == typeof(byte[]) => new ByteContentContainer(s_streamManager, name, content as byte[],
                compress),
            { } t when t == typeof(Stream) => new StreamContentContainer(s_streamManager, name, content as Stream,
                compress),
            _ => null
        };

    private async Task<ValidationResult> ValidateFileAsync(DropBearFile file)
    {
        var validationTasks = new List<Task<ValidationResult>>
        {
            _strategyValidator.ValidateAsync(file),
            // Add other validations as needed
        };

        var validationResults = await Task.WhenAll(validationTasks).ConfigureAwait(false);
        var aggregatedResult = validationResults.Aggregate(ValidationResult.Success(),
            (current, result) => current.Combine(result));

        return aggregatedResult;
    }

    // Protected implementation of Dispose pattern.
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
