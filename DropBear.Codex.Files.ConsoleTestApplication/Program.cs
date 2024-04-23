using DropBear.Codex.Files.Builders;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Services;
using DropBear.Codex.Serialization.Providers;
using DropBear.Codex.Serialization.Serializers;
using MessagePack;

namespace DropBear.Codex.Files.ConsoleTestApplication;

internal class Program
{
#pragma warning disable CA1416
    public static async Task Main(string[] args)
    {
        var fileManager = new FileManagerBuilder()
            .Build();

        // Asynchronously create a content container
        var contentContainer = await CreateTestContentContainer();

        var dropBearFile = new DropBearFileBuilder()
            .SetFileName("test")
            .SetBaseFilePath(@"C:\Temp")
            .AddMetadata("Author", "John Doe")
            .AddContentContainer(contentContainer)
            .SetInitialVersion("v1.0", DateTimeOffset.UtcNow)
            .Build();

        await FileManager.WriteToFileAsync(dropBearFile);
        var readBackDropBearFile = await FileManager.ReadFromFileAsync(dropBearFile.FullPath);

        //DropBearFileComparer.CompareDropBearFiles(dropBearFile, readBackDropBearFile);

        //TestTypeSerialization();

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