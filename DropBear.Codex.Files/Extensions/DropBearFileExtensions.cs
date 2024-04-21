using System.Text.Json;
using System.Text.Json.Serialization;
using DropBear.Codex.Files.Converters;
using DropBear.Codex.Files.Models;
using Microsoft.IO;

namespace DropBear.Codex.Files.Extensions;

public static class DropBearFileExtensions
{
    private static readonly RecyclableMemoryStreamManager StreamManager = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new TypeConverter() },
        IncludeFields = true
    };

    public static Stream ToStream(this DropBearFile file)
    {
        Stream stream = StreamManager.GetStream("DropBearFileToStream");
        var writerOptions = new JsonWriterOptions { Indented = true };
        using (var writer = new Utf8JsonWriter(stream, writerOptions))
        {
            JsonSerializer.Serialize(writer, file, Options);
        }

        stream.Position = 0; // Reset the position for reading
        return stream;
    }

    public static DropBearFile FromStream(Stream stream)
    {
        try
        {
            return JsonSerializer.Deserialize<DropBearFile>(stream, Options) ??
                   throw new InvalidOperationException("Failed to deserialize DropBearFile from stream.");
        }
        catch (JsonException e)
        {
            Console.WriteLine($"Deserialization error: {e.Message}");
            throw;
        }
    }
    
    public static string ToJsonString(this DropBearFile file)
    {
        return JsonSerializer.Serialize(file, Options);
    }

}
