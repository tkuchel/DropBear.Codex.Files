using DropBear.Codex.AppLogger.Builders;
using DropBear.Codex.Files.Factory.Implementations;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.MessagePackChecker;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Models.FileComponents.MainComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using DropBear.Codex.Files.Validation.Strategies;
using DropBear.Codex.Utilities.MessageTemplates;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;
using DropBear.Codex.Validation.StrategyValidation.Services;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ZLogger;
using ILoggerFactory = DropBear.Codex.AppLogger.Interfaces.ILoggerFactory;

namespace DropBear.Codex.Files.Factory;

public static class FileManagerFactory
{
    private static RecyclableMemoryStreamManager? StreamManager;
    private static IStrategyValidator? StrategyValidator;

    private static bool s_fileManagerFactoryInitialized;

    private static readonly ILogger<FileManagerFactoryLogger>? Logger =
        LoggerFactory?.CreateLogger<FileManagerFactoryLogger>();

    private static ILoggerFactory? LoggerFactory { get; set; }
    private static MessageTemplateManager? MessageTemplateManager { get; set; }

    public static IFileCreator FileCreator() => new FileCreator(StreamManager, LoggerFactory, StrategyValidator);
    public static IFileWriter FileWriter() => new FileWriter(StreamManager, LoggerFactory);
    public static IFileReader FileReader() => new FileReader(StreamManager, LoggerFactory);
    public static IFileDeleter FileDeleter() => new FileDeleter(StreamManager);
    public static IFileUpdater FileUpdater() => new FileUpdater(StreamManager);
    public static IFileValidator FileValidator() => new FileValidator(StreamManager);
    public static IFileIntegrityChecker FileIntegrityChecker() => new FileIntegrityChecker(StreamManager);

    public static async Task InitializeAsync()
    {
        if (s_fileManagerFactoryInitialized)
        {
            Logger?.ZLogWarning($"FileManager already initialized.");
            return;
        }

        // Initialize the required services
        StreamManager = new RecyclableMemoryStreamManager();
        StrategyValidator = new StrategyValidator();
        MessageTemplateManager = new MessageTemplateManager();
        LoggerFactory = new LoggerConfigurationBuilder()
            .SetLogLevel(LogLevel.Information)
            .EnableConsoleOutput()
            .UseJsonFormatter() // Assuming you want JSON formatted logs
            .ConfigureRollingFile("logs/", 1024) // Configure rolling file and size
            .Build();

        // Initialize the FileManager
        Logger?.ZLogInformation($"Initializing FileManager...");
        await RegisterValidationStrategiesAsync().ConfigureAwait(false);
        await RegisterMessageTemplatesAsync().ConfigureAwait(false);

        var compatibilityResult = await CheckMessagePackCompatibilityAsync().ConfigureAwait(false);
        if (compatibilityResult.IsValid)
            Logger?.ZLogInformation($"MessagePack compatibility check passed.");
        else
            Logger?.ZLogError(
                $"MessagePack compatibility check failed: {compatibilityResult.Errors.Count()} errors found.");

        s_fileManagerFactoryInitialized = true;
        Logger?.ZLogInformation($"FileManager initialized successfully.");
    }

    private static Task RegisterValidationStrategiesAsync()
    {
        // Register validation strategies with the StrategyValidator service
        _logger.LogInformation("Registering validation strategies.");
        StrategyValidator.RegisterStrategy(new FileContentValidationStrategy());
        StrategyValidator.RegisterStrategy(new FileHeaderValidationStrategy());
        StrategyValidator.RegisterStrategy(new FileMetaDataValidationStrategy());
        StrategyValidator.RegisterStrategy(new DropBearFileValidationStrategy());
        _logger.LogInformation("Validation strategies registered successfully.");
        return Task.CompletedTask;
    }

    private static Task RegisterMessageTemplatesAsync()
    {
        // Register message templates with the MessageTemplateManager service
        _logger.LogInformation("Registering message templates.");
        MessageTemplateManager.RegisterTemplates(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "TestTemplateId", "Test template id: {0}" }
        });
        _logger.LogInformation("Message templates registered successfully.");
        return Task.CompletedTask;
    }

    private static Task<ValidationResult> CheckMessagePackCompatibilityAsync()
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

    private record FileManagerFactoryLogger
    {
    };
}
