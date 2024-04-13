using DropBear.Codex.Files.Factory;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using DropBear.Codex.Utilities.Helpers;
using FastRsync.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using Microsoft.IO;

namespace DropBear.Codex.Files.Utils;

public class ContentContainerConverter : JsonConverter
{
    private ContentContainerFactory ContentContainerFactory { get; }
    private RecyclableMemoryStreamManager _streamManager = FileManagerFactory.StreamManager;

    public ContentContainerConverter()
    {
        ContentContainerFactory = new ContentContainerFactory(FileManagerFactory.StreamManager); }
    

    public override bool CanConvert(Type objectType) => typeof(IContentContainer).IsAssignableFrom(objectType);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        var name = obj["Name"].Value<string>();
        var compress = obj["IsCompressed"].Value<bool>();

        // Deserialize the ContentTypeInfo using the JsonSerializer
        var contentType = obj["ContentType"].ToObject<ContentTypeInfo>(serializer);
    
        if (contentType == null)
        {
            throw new JsonSerializationException("ContentTypeInfo cannot be null");
        }

        switch (contentType.TypeName)
        {
            case "Byte[]":
                var contentBytes = obj["Content"].Value<string>().GetBytes();
                var content = DecompressIfNeeded(contentBytes, false); // Temporarily set to false
                return ContentContainerFactory.CreateByteContentContainer(name, content, compress);
            default:
                throw new JsonSerializationException("Unknown content type for IContentContainer");
        }
    }


    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
        throw new NotImplementedException("Serialization of IContentContainer is not supported.");
    
    private byte[] DecompressIfNeeded(byte[] compressedContent, bool wasCompressed)
    {
        if (!wasCompressed) return compressedContent;

        try
        {
            using var compressedStream = new MemoryStream(compressedContent);
            using var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = _streamManager.GetStream();
        
            decompressionStream.CopyTo(resultStream);
            resultStream.Position = 0;
        
            // Assuming ReadStream is a method to read all bytes from the stream
            return ReadStream(resultStream);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to decompress content.", ex);
        }
    }
    
    private byte[] ReadStream(Stream input)
    {
        using var memoryStream = _streamManager.GetStream();
        input.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

}
