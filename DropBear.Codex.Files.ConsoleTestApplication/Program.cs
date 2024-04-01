using DropBear.Codex.AppLogger.Extensions;
using DropBear.Codex.AppLogger.Interfaces;
using DropBear.Codex.Files.ConsoleTestApplication.Models;
using DropBear.Codex.Files.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DropBear.Codex.Files.ConsoleTestApplication;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Create a new service collection
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        // Build the service provider
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Resolve the services that we need
        var logger = serviceProvider.GetRequiredService<IAppLogger<Program>>();
        var fileManager = serviceProvider.GetRequiredService<IFileManager>();

        logger.LogInformation($"Started at {DateTimeOffset.UtcNow}");


        // Log that we are finished
        logger.LogInformation($"Finished at {DateTimeOffset.UtcNow}");

        // Dispose of the service provider
        await serviceProvider.DisposeAsync();
    }


    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddAppLogger();
        services.AddDropBearCodexFiles();
    }

    private static TestData CreateTestData()
    {
        return new TestData
            { Id = 1, Name = "TestDataClass", Description = "This is a test data class for file testing" };
    }
}