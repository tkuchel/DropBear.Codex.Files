using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.ContentContainerStrategies;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Models.ContentContainers;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ZLogger;
using ILoggerFactory = DropBear.Codex.AppLogger.Interfaces.ILoggerFactory;

namespace DropBear.Codex.Files.Factory.Implementations;

public sealed class FileCreator : IFileCreator
{
    private readonly ILogger<FileCreator> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IStrategyValidator _strategyValidator;
    private readonly RecyclableMemoryStreamManager _streamManager;
    private readonly List<IContentContainerStrategy> _strategies;
    private bool _useCompression;

    public FileCreator(RecyclableMemoryStreamManager? streamManager, ILoggerFactory? loggerFactory,
        IStrategyValidator? strategyValidator)
    {
        _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        _strategyValidator = strategyValidator ?? throw new ArgumentNullException(nameof(strategyValidator));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<FileCreator>();
        
        // Initialize strategies
        _strategies = new List<IContentContainerStrategy>
        {
            new StringContentContainerStrategy(),
            new ByteContentContainerStrategy(),
            new StreamContentContainerStrategy(),
            // Add other strategies here
        };
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
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(type));
        return strategy?.CreateContentContainer(name, content, compress, _streamManager);
    }

    private async Task<ValidationResult> ValidateFileAsync(DropBearFile file)
    {
        var results = await _strategyValidator.ValidateAsync(file).ConfigureAwait(false);
        return results; // Assuming ValidateAsync returns a ValidationResult
    }
    
}
