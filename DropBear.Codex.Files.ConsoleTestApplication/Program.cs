using System.Text;
using System.Text.Json;
using DropBear.Codex.Files.Builders;
using DropBear.Codex.Files.Converters;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Services;
using DropBear.Codex.Serialization.Providers;
using MessagePack;

namespace DropBear.Codex.Files.ConsoleTestApplication;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var fileManager = new FileManager().Build();

        // Asynchronously create a content container
        var contentContainer = await CreateTestContentContainer();

        var dropBearFile = new DropBearFileBuilder()
            .SetFileName("test")
            .SetBaseFilePath(@"C:\Temp")
            .AddMetadata("Author", "John Doe")
            .AddContentContainer(contentContainer)
            .SetInitialVersion("v1.0", DateTimeOffset.UtcNow)
            .Build();

        await fileManager.WriteToFileAsync(dropBearFile);
        var readBackDropBearFile = await fileManager.ReadFromFileAsync(dropBearFile.FullPath);
        
        DropBearFileComparer.CompareDropBearFiles(dropBearFile,readBackDropBearFile);

        //TestTypeSerialization();
        
        if (readBackDropBearFile.ContentContainers.Any())
        {
            var rawData = await readBackDropBearFile.ContentContainers[0].GetRawDataAsync();
            if (!rawData.IsSuccess)
            {
                Console.WriteLine("Failed to read raw data from content container.");
                return;
            }

            var containerContent = Encoding.UTF8.GetString(rawData.Value);
            Console.WriteLine("Read back content: " + containerContent);
        }
        else
        {
            Console.WriteLine("No content containers were found in the read file.");
        }
    }

    private static async Task<ContentContainer> CreateTestContentContainer()
    {
        var content = "Hello, world!";
        var serializedContent = MessagePackSerializer.Serialize(content);
        return await new ContentContainerBuilder()
            .WithData(serializedContent)
            .WithCompression(new GZipCompressionProvider())
            .BuildAsync();
    }
    
    public static void TestTypeSerialization()
    {
        Type testType = typeof(DropBearFile);
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters = { new TypeConverter() },
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(testType, options);
        Console.WriteLine("Serialized type: " + json);

        Type deserializedType = JsonSerializer.Deserialize<Type>(json, options);
        Console.WriteLine("Deserialized type: " + deserializedType);
    }

}

[MessagePackObject(keyAsPropertyName: true)]
public class TestFile
{
    public TestFile()
    {
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