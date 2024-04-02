using DropBear.Codex.AppLogger.Extensions;
using DropBear.Codex.AppLogger.Interfaces;
using DropBear.Codex.Files.ConsoleTestApplication.Models;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Services;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Text;

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

        // Create a test data object
        var testData = CreateTestData();

        // Create a DropBearFile object
        var dropBearFile = await fileManager.CreateFileAsync("TestFile", testData);
        dropBearFile.Dump();

        // Write the file to disk
        if (dropBearFile != null) await fileManager.WriteFileAsync(dropBearFile, @"C:\Temp");

        // Read the file from disk
        var readDropBearFile = await fileManager.ReadFileAsync(@"C:\Temp\TestFile.dbf");
        readDropBearFile.Dump();

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