using DropBear.Codex.Files.Factory;
using DropBear.Codex.Files.Factory.Implementations;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Utilities.Extensions;
using DropBear.Codex.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;

namespace DropBear.Codex.Files;

/// <summary>
///     Extensions for configuring services related to DropBear Codex files.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the DropBear Codex file services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
    /// <remarks>
    ///     This method adds logging, validation, serialization, and file management services to the service collection.
    /// </remarks>
    public static void AddDropBearCodexFiles(this IServiceCollection services)
    {
        // Add validation services
        services.AddValidationServices();

        // Add MessageTemplateManager service
        services.AddMessageTemplateManager();

        // Add RecyclableMemoryStreamManager service
        services.AddSingleton<RecyclableMemoryStreamManager>(); // If not already registered
        services.AddTransient<IContentContainerFactory, ContentContainerFactory>();
        services.AddTransient<IFileCreator, FileCreator>();
        services.AddTransient<IFileReader, FileReader>();
        services.AddTransient<IFileDeleter, FileDeleter>();
        services.AddTransient<IFileUpdater, FileUpdater>();
        services.AddTransient<IFileDeltaUtility, FileDeltaUpdater>();
    }
}
