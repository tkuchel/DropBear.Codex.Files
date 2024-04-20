using System.Text;
using DropBear.Codex.Files.Builders;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Services;
using DropBear.Codex.Serialization.Providers;
using Kokuban;
using MessagePack;

namespace DropBear.Codex.Files.ConsoleTestApplication;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var fileManager = new FileManager()
            .ConfigureLocalPath("C:\\Temp\\DropBearFiles")
            .Build();

        var contentContainer = new ContentContainerBuilder()
            .WithData(Encoding.UTF8.GetBytes("Hello, world!"))
            .BuildAsync();

        var dropBearFile = new DropBearFileBuilder()
            .AddMetadata("Author", "John Doe")
            .AddContentContainer(await CreateTestContentContainer())
            .SetInitialVersion("v1.0", DateTime.UtcNow, "path/to/delta", "path/to/signature")
            .Build();

        var filePath = "test.dbb";
        await fileManager.WriteToFileAsync(dropBearFile, filePath);

        var readBackDropBearFile = await fileManager.ReadFromFileAsync(filePath);
        if (readBackDropBearFile.ContentContainers.Any())
        {
            var containerContent = Encoding.UTF8.GetString(readBackDropBearFile.ContentContainers.First().Data.ToArray<byte>());
            Console.WriteLine("Read back content: " + containerContent);
        }
        else
        {
            Console.WriteLine("No content containers were found in the read file.");
        }
    }


    private static async Task<ContentContainer> CreateTestContentContainer()
    {
        // Create a test content using ContentContainerBuilder
        var content = "Hello, world!";
        var serializedContent = MessagePackSerializer.Serialize(content);
        var builder = new ContentContainerBuilder();

        var container = await builder
            .WithData(serializedContent)
            .WithCompression(new GZipCompressionProvider()) 
            .BuildAsync();
        
        return container;
    }
}

[MessagePackObject]
public class TestFile
{
    public TestFile()
    {
    }

    public TestFile(string name, DateTime createdAt, string content)
    {
        Name = name;
        CreatedAt = createdAt;
        Content = content;
    }

    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Content { get; set; }
}