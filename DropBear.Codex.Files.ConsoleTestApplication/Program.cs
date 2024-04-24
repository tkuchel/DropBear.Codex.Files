using DropBear.Codex.Files.Builders;
using DropBear.Codex.Files.Enums;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Serialization.Providers;
using DropBear.Codex.Serialization.Serializers;
using MessagePack;
using Microsoft.IO;

namespace DropBear.Codex.Files.ConsoleTestApplication;

internal class Program
{
#pragma warning disable CA1416
    public static async Task Main(string[] args)
    {
        var accountKey = "YOUR_ACCOUNT_KEY";
        var accountName = "YOUR_ACCOUNT_NAME";
        var containerName = "YOUR_CONTAINER_NAME";

        var fileManager = FileManagerBuilder.Create()
            .WithMemoryStreamManager(new RecyclableMemoryStreamManager())
            .WithBlobStorage(accountName, accountKey, containerName)
            .WithStorageStrategy(StorageStrategy.BlobOnly)
            .Configure()
            .Build();
        
        // var fileManager = FileManagerBuilder.Create()
        //     .WithMemoryStreamManager(new RecyclableMemoryStreamManager())
        //     .WithBlobStorage(accountName, accountKey, containerName)
        //     .WithLocalStorage("C:\\Temp")
        //     .WithStorageStrategy(StorageStrategy.Both)
        //     .Configure()
        //     .Build();


        // Asynchronously create a content container
        var contentContainer = await CreateTestContentContainer();

        var dropBearFile = new DropBearFileBuilder()
            .SetFileName("test")
            .AddMetadata("Author", "John Doe")
            .AddContentContainer(contentContainer)
            .SetInitialVersion("v1.0", DateTimeOffset.UtcNow)
            .Build();

        var fullLocalPath = Path.Combine("C:\\Temp", dropBearFile.FileName + DropBearFile.GetDefaultExtension());
        var fullBlobPath = Path.Combine(containerName, dropBearFile.FileName + DropBearFile.GetDefaultExtension());

        await fileManager.WriteToFileAsync(dropBearFile, fullBlobPath);
        var readBackDropBearFile = await fileManager.ReadFromFileAsync(fullBlobPath);

        DropBearFileComparer.CompareDropBearFiles(dropBearFile, readBackDropBearFile);

        if (readBackDropBearFile.ContentContainers.Any())
        {
            var containerData = await readBackDropBearFile.ContentContainers.First().GetDataAsync<TestFile>();
            if (!containerData.IsSuccess)
            {
                Console.WriteLine("Failed to get data from content container.");
            }
            else
            {
                var result = containerData.Value;
                Console.WriteLine("Data from content container: " + result.Name + " " + result.CreatedAt + " " +
                                  result.Content);
            }
        }
        else
        {
            Console.WriteLine("No content containers were found in the read file.");
        }
    }

    private static async Task<ContentContainer> CreateTestContentContainer()
    {
        var content = new TestFile
        {
            Name = "NotATest",
            CreatedAt = DateTimeOffset.UtcNow,
            Content = "This is not a test file."
        };
        //var serializedContent = MessagePackSerializer.Serialize(content);
        //var deserializedContent = MessagePackSerializer.Deserialize<TestFile>(serializedContent);
        return await new ContentContainerBuilder()
            .WithObject(content)
            .WithSerializer<JsonSerializer>()
            .WithCompression<GZipCompressionProvider>()
            .WithEncryption<AESGCMEncryptionProvider>()
            .BuildAsync();
    }
#pragma warning restore CA1416
}

[MessagePackObject(true)]
public class TestFile
{
    public TestFile()
    {
        Name = string.Empty;
        CreatedAt = DateTimeOffset.MinValue;
        Content = string.Empty;
    }

    public TestFile(string name, DateTimeOffset createdAt, string content)
    {
        Name = name;
        CreatedAt = createdAt;
        Content = content;
    }

    public string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Content { get; set; }
}