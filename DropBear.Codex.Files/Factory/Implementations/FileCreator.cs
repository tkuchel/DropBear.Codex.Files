using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Models.ContentContainers;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ZLogger;

namespace DropBear.Codex.Files.Factory.Implementations;

public sealed class FileCreator : IFileCreator, IDisposable
{
    private readonly ILogger<FileCreator> _logger;
    private readonly IStrategyValidator _strategyValidator;
    private readonly RecyclableMemoryStreamManager _streamManager;
    private bool _disposed;
    private bool _useCompression;

    public FileCreator(RecyclableMemoryStreamManager streamManager, ILogger<FileCreator> logger,
        IStrategyValidator strategyValidator)
    {
        _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        _strategyValidator = strategyValidator ?? throw new ArgumentNullException(nameof(strategyValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IFileCreator WithCompression()
    {
        _useCompression = true;
        return this;
    }

    public async Task<Result<DropBearFile>> CreateAsync<T>(string name, T content,
        bool forceCreation = false) where T : class
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.ZLogError($"Name cannot be null or empty");
            return Result<DropBearFile>.Failure("Name cannot be null or empty");
        }

        name = Path.GetFileNameWithoutExtension(name);

        try
        {
            var contentContainer = CreateContentContainer(name, content, _useCompression, typeof(T));

            if (contentContainer is null)
            {
                _logger.ZLogError($"Error creating content container for {name}");
                return Result<DropBearFile>.Failure($"Error creating content container for {name}");
            }

            var dropBearFile = new DropBearFile(name, Environment.UserName, _useCompression);
            dropBearFile.AddContent(contentContainer);

            var validationResult = await ValidateFileAsync(dropBearFile).ConfigureAwait(false);
            if (validationResult.IsValid || forceCreation) return Result<DropBearFile>.Success(dropBearFile);

            _logger.ZLogWarning($"File validation failed: {string.Join(", ", validationResult.Errors)}");
            return Result<DropBearFile>.Failure("Validation failed.");
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"Error creating file {name}");
            return Result<DropBearFile>.Failure($"Error creating file {name}");
        }
    }

    private IContentContainer? CreateContentContainer<T>(string name, T content, bool compress, Type type)
        where T : class
    {
        switch (type)
        {
            case { } t when t == typeof(string):
                return new StringContentContainer(_streamManager, name, content as string, compress);
            case { } t when t == typeof(byte[]):
                return new ByteContentContainer(_streamManager, name, content as byte[], compress);
            case { } t when t == typeof(Stream):
                return new StreamContentContainer(_streamManager, name, content as Stream, compress);
            default:
                return null;
        }
    }

    private async Task<ValidationResult> ValidateFileAsync(DropBearFile file)
    {
        var results = await _strategyValidator.ValidateAsync(file).ConfigureAwait(false);
        return results; // Assuming ValidateAsync returns a ValidationResult
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Managed resources to dispose
        }

        _disposed = true;
    }
}
