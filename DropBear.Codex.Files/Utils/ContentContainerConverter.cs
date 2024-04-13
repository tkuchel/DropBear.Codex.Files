using DropBear.Codex.Files.Factory;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DropBear.Codex.Files.Utils;

public class ContentContainerConverter : JsonConverter
{
    private ContentContainerFactory ContentContainerFactory { get; }

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
                var content = Convert.FromBase64String(obj["Content"].Value<string>());
                return ContentContainerFactory.CreateByteContentContainer(name, content, compress);
            default:
                throw new JsonSerializationException("Unknown content type for IContentContainer");
        }
    }


    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
        throw new NotImplementedException("Serialization of IContentContainer is not supported.");
}
