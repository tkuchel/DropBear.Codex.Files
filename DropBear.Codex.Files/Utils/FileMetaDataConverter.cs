using System.Collections.ObjectModel;
using DropBear.Codex.Files.Models.FileComponents.MainComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DropBear.Codex.Files.Utils;

public class FileMetadataConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(FileMetadata);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        var fileMetadata = existingValue as FileMetadata ?? new FileMetadata();

        // Handling string properties
        fileMetadata.FileName = obj["FileName"].Value<string>();
        fileMetadata.FileSize = obj["FileSize"].Value<int>();
        fileMetadata.FileOwner = obj["FileOwner"].Value<string>();

        // Handling DateTimeOffset properties
        fileMetadata.FileCreatedDate = GetDateTimeOffset(obj["FileCreatedDate"]);
        fileMetadata.FileModifiedDate = GetDateTimeOffset(obj["FileModifiedDate"]);

        // Deserialize ContentTypes
        var contentTypes = obj["ContentTypes"].ToObject<Collection<ContentTypeInfo>>(serializer);
        foreach (var contentType in contentTypes)
        {
            if (fileMetadata.ContentTypes.All(ct => ct.TypeName != contentType.TypeName))
                fileMetadata.ContentTypes.Add(contentType);
        }

        // Deserialize ContentTypeVerificationHashes
        var hashes = obj["ContentTypeVerificationHashes"].ToObject<Dictionary<string, string>>(serializer);
        foreach (var hash in hashes)
        {
            fileMetadata.ContentTypeVerificationHashes[hash.Key] = hash.Value;
        }

        // Deserialize CustomMetadata if it exists in the JSON
        var customMetadataToken = obj["CustomMetadata"];
        if (customMetadataToken is null) return fileMetadata;
        var customMetadata = customMetadataToken.ToObject<Dictionary<string, string>>(serializer);
        if (customMetadata is null) return fileMetadata;
        foreach (var metadata in customMetadata)
        {
            fileMetadata.AddOrUpdateCustomMetadata(metadata.Key, metadata.Value);
        }


        return fileMetadata;
    }

    private DateTimeOffset GetDateTimeOffset(JToken token)
    {
        if (token.Type is not JTokenType.Date) throw new JsonSerializationException("Invalid date format");
        if (token.ToObject<DateTimeOffset>() is DateTimeOffset dto)
        {
            return dto;
        }
        if (token.ToObject<DateTime>() is DateTime dt)
        {
            return new DateTimeOffset(dt);
        }
        throw new JsonSerializationException("Invalid date format");
    }


    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
        throw new NotImplementedException("Serialization of FileMetadata is not supported.");
}
