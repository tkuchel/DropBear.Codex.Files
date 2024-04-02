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

        // Create a test data object
        var testData = CreateTestData();
        const string path = @"C:\Temp\";

        // Create a DropBearFile object
        var dropBearFile = await fileManager.CreateFileAsync("TestFile", testData);

        var fullPathAndNameWithExt = path + dropBearFile?.GetFileNameWithExtension();

        // Write the file to disk
        if (dropBearFile != null) await fileManager.WriteFileAsync(dropBearFile, path);

        // Read the file from disk
        var readDropBearFile = await fileManager.ReadFileAsync(fullPathAndNameWithExt);

        // Update the file
        if (readDropBearFile != null)
        {
            readDropBearFile.Metadata.FileOwner = "UpdatedOwner";

            await fileManager.UpdateFile(fullPathAndNameWithExt, readDropBearFile);
        }
        
        // Read the file from disk
        var readDropBearFileUpdated = await fileManager.ReadFileAsync(fullPathAndNameWithExt);
        
        // Lets check the file owner was updated to UpdatedOwner
        if (readDropBearFileUpdated != null)
        {
            logger.LogInformation($"File Owner: {readDropBearFileUpdated.Metadata.FileOwner}");
        }
        
        // Delete the file
        fileManager.DeleteFile(fullPathAndNameWithExt);

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