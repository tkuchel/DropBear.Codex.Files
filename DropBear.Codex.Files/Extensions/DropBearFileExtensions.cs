using System.Text;
using DropBear.Codex.Files.Models;
using Microsoft.IO;
using Newtonsoft.Json;

namespace DropBear.Codex.Files.Extensions;

public static class DropBearFileExtensions
{
    private static readonly RecyclableMemoryStreamManager StreamManager = new();

    public static Stream ToStream(this DropBearFile file)
    {
        var stream = StreamManager.GetStream("DropBearFileToStream");
        using (var streamWriter = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 1024, leaveOpen: true))
        using (var jsonWriter = new JsonTextWriter(streamWriter))
        {
            var serializer = new JsonSerializer();
            serializer.Serialize(jsonWriter, file);
            jsonWriter.Flush(); // Ensure all data is written to the stream
        }

        stream.Position = 0; // Reset the position for reading
        return stream;
    }

    public static DropBearFile FromStream(Stream stream)
    {
        using var streamReader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(streamReader);
        var serializer = new JsonSerializer();
        return serializer.Deserialize<DropBearFile>(jsonReader) ??
               throw new InvalidOperationException("Failed to deserialize DropBearFile from stream.");
    }
}
