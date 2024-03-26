using DropBear.Codex.AppLogger.Extensions;
using DropBear.Codex.Serialization;
using DropBear.Codex.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace DropBear.Codex.Files;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the DropBear Codex file services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
    public static void AddDropBearCodexFiles(this IServiceCollection services)
    {
        services.AddAppLogger();
        services.AddValidationServices();
        services.AddDataSerializationServices();
    }
}
