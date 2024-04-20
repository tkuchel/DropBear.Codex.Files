using DropBear.Codex.Files.Builders;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Services;
using DropBear.Codex.Serialization.Providers;
using Kokuban;
using MessagePack;

namespace DropBear.Codex.Files.ConsoleTestApplication;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine(Chalk.Blue + "Starting test application");

        // Configuration for FileManager
        var fileManager = new FileManager()
            .ConfigureLocalPath("C:\\Temp\\DropBearFiles")
            .ConfigureBlobStorage("yourAccountName", "yourAccountKey")
            .Build();

        // Create a test file using DropBearFileBuilder
        var dropBearFile = new DropBearFileBuilder()
            .AddMetadata("Author", "John Doe")
            .AddContentContainer(await CreateTestContentContainer())
            .Build();

        // Save to local file system
        var localFilePath = "test_file.dbb";
        await fileManager.WriteToFileAsync(dropBearFile, localFilePath);
        Console.WriteLine(Chalk.Green + "File written to local storage.");

        // Optionally, read back the file
        var readBackDropBearFile = await fileManager.ReadFromFileAsync(localFilePath);
        Console.WriteLine(Chalk.Yellow + "Read back file content: " +
                          readBackDropBearFile.ContentContainers[0].Content);

        Console.WriteLine(Chalk.Blue + "End of test application");
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