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
using DropBear.Codex.Validation.StrategyValidation.Services;
using ServiceStack.Text;
using ZLogger;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using ILoggerFactory = DropBear.Codex.AppLogger.Interfaces.ILoggerFactory;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace DropBear.Codex.Files.Factory;

public static class FileManagerFactory
{
    private static RecyclableMemoryStreamManager? s_streamManager;
    private static StrategyValidator? s_strategyValidator;
    private static ILoggerFactory? s_loggerFactory;
    private static MessageTemplateManager? s_messageTemplateManager;
    private static ILogger? s_logger; // Added logger for initialization results

    // Factory methods
    public static IFileCreator FileCreator()
    {
        FileManagerInitializer.EnsureInitialized(s_loggerFactory);
        return new FileCreator(s_streamManager, s_loggerFactory, s_strategyValidator);
    }

    public static IFileWriter FileWriter()
    {
        FileManagerInitializer.EnsureInitialized(s_loggerFactory);
        return new FileWriter(s_streamManager, s_loggerFactory);
    }

    public static IFileReader FileReader()
    {
        FileManagerInitializer.EnsureInitialized(s_loggerFactory);
        return new FileReader(s_streamManager, s_loggerFactory);
    }

    public static IFileDeleter FileDeleter()
    {
        FileManagerInitializer.EnsureInitialized(s_loggerFactory);
        return new FileDeleter(s_loggerFactory);
    }

    public static IFileUpdater FileUpdater()
    {
        FileManagerInitializer.EnsureInitialized(s_loggerFactory);
        return new FileUpdater(s_streamManager, s_loggerFactory);
    }

    public static IFileDeltaUtility FileDeltaUtility()
    {
        FileManagerInitializer.EnsureInitialized(s_loggerFactory);
        return new FileDeltaUtility(s_streamManager, s_loggerFactory);
    }

    private class FileManagerInitializer
    {
        private static bool s_isInitialized;
        private static readonly object InitLock = new();

        public static void EnsureInitialized(ILoggerFactory? loggerFactory)
        {
            if (s_isInitialized) return;
            lock (InitLock)
            {
                if (s_isInitialized) return;
                InitializeComponents(loggerFactory);
                s_isInitialized = true;
            }
        }

        private static void InitializeComponents(ILoggerFactory? loggerFactory)
        {
            try
            {
                s_streamManager = new RecyclableMemoryStreamManager();
                s_strategyValidator = new StrategyValidator();
                s_messageTemplateManager = new MessageTemplateManager();
                s_loggerFactory = loggerFactory ?? BuildDefaultLoggerFactory();
                s_logger = s_loggerFactory!.CreateLogger<FileManagerFactoryLogger>();

                RegisterValidationStrategies();
                RegisterMessageTemplates();

                var compatibilityResult = CheckMessagePackCompatibility();
                LogInitializationResults(compatibilityResult);
            }
            catch (Exception ex)
            {
                // Enhanced error handling
                s_logger?.ZLogError(ex, $"Error during FileManagerFactory initialization.");
                throw; // Consider whether to swallow or rethrow based on your application's needs
            }
        }

        private static ILoggerFactory BuildDefaultLoggerFactory() =>
            // Logger configuration logic here
            new LoggerConfigurationBuilder()
                .SetLogLevel(LogLevel.Information)
                .EnableConsoleOutput()
                .UseJsonFormatter()
                .ConfigureRollingFile("logs/", 1024)
                .Build();

        private static void RegisterValidationStrategies()
        {
            // Register validation strategies with the StrategyValidator service

            s_strategyValidator?.RegisterStrategy(new FileContentValidationStrategy());
            s_strategyValidator?.RegisterStrategy(new FileHeaderValidationStrategy());
            s_strategyValidator?.RegisterStrategy(new FileMetaDataValidationStrategy());
            s_strategyValidator?.RegisterStrategy(new DropBearFileValidationStrategy());
        }

        private static void RegisterMessageTemplates() =>
            // Register message templates with the MessageTemplateManager service
            s_messageTemplateManager?.RegisterTemplates(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "TestTemplateId", "Test template id: {0}" }
            });

        private static ValidationResult CheckMessagePackCompatibility()
        {
            // Check each class used in the DropBearFile and its subcomponents for compatibility with MessagePack serialization
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
            if (results.FailedTypes.Count is 0) return ValidationResult.Success();

            var validationResult = ValidationResult.Success();
            foreach (var (type, reason) in results.FailedTypes) validationResult.AddError(type, reason);

            return validationResult;
        }

        private static void LogInitializationResults(ValidationResult compatibilityResult)
        {
            if (compatibilityResult.IsValid)
                s_logger?.ZLogInformation($"MessagePack compatibility check passed.");
            else
                s_logger?.ZLogError($"MessagePack compatibility check failed: {compatibilityResult.Errors.Count()} errors found.");
        }
    }

    private class FileManagerFactoryLogger();
}
