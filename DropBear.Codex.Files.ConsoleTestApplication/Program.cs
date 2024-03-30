using DropBear.Codex.AppLogger.Extensions;
using DropBear.Codex.AppLogger.Interfaces;
using DropBear.Codex.Files.ConsoleTestApplication.Models;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Serialization;
using DropBear.Codex.Serialization.Enums;
using DropBear.Codex.Serialization.Interfaces;
using DropBear.Codex.Utilities.Helpers;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Text;

namespace DropBear.Codex.Files.ConsoleTestApplication;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Create a new service collection
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddAppLogger();
        serviceCollection.AddDropBearCodexFiles();
        serviceCollection.AddDataSerializationServices();

        // Build the service provider
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<IAppLogger<Program>>();
        var fileManager = serviceProvider.GetRequiredService<IFileManager>();
        var fileContentFactory = serviceProvider.GetRequiredService<IFileContentFactory>();
        var contentContainerFactory = serviceProvider.GetRequiredService<IContentContainerFactory>();
        var dataSerializer = serviceProvider.GetRequiredService<IDataSerializer>();

        var testData = new TestData
            { Id = 1, Name = "TestDataClass", Description = "This is a test data class for file testing" };
        const string author = "Terrence Kuchel";

        var fileData =
            await dataSerializer.SerializeJsonAsync(testData, CompressionOption.Compressed, EncodingOption.Base64);

        if (fileData is not null && fileData.IsFailure)
        {
            //Console.WriteLine($"[+] Failure at {DateTimeOffset.UtcNow} {fileData.ErrorMessage}");
            logger.LogError($"Failure at {DateTimeOffset.UtcNow} {fileData.ErrorMessage}");
            return;
        }

        if (fileData is not null)
        {
            var fileBytes = fileData.Value.GetBytes();
            var contentContainer = contentContainerFactory.Create(fileBytes, typeof(TestData));
            var fileContent = fileContentFactory.Create();
            fileContent.ClearContents();
            fileContent.AddContent(contentContainer);

            var result = await fileManager.CreateFileAsync(author, fileContent, true);
            if (result.IsFailure)
            {
                //Console.WriteLine($"[+] Failure at {DateTimeOffset.UtcNow} {result.ErrorMessage}");
                logger.LogError($"Failure at {DateTimeOffset.UtcNow} {result.ErrorMessage}");
                return;
            }

            var writeFileToDisk = await fileManager.WriteFileAsync(result.Value, @"C:\Temp\TestData.dbf");

            logger.LogInformation($"File write result: {writeFileToDisk.Dump()}");
            logger.LogInformation($"Finished at {DateTimeOffset.UtcNow}");
        }
        else
        {
            logger.LogError($"Failure at {DateTimeOffset.UtcNow} {fileData?.ErrorMessage}");
        }
    }
}