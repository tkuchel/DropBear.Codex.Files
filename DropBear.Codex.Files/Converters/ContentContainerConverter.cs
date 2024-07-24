#region

using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using DropBear.Codex.Files.Enums;
using DropBear.Codex.Files.Models;

#endregion

namespace DropBear.Codex.Files.Converters;

[SupportedOSPlatform("windows")]
public class ContentContainerConverter : JsonConverter<ContentContainer>
{
    public override ContentContainer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var container = new ContentContainer();
        Dictionary<string, Type> providers = new(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.EndObject:
                    container.SetProviders(providers); // Set the providers after all are read
                    return container;
                case JsonTokenType.PropertyName:
                {
                    var propertyName = reader.GetString();
                    reader.Read(); // Move to the value token
                    switch (propertyName)
                    {
                        case "flags":
                            container.EnableFlag((ContentContainerFlags)reader.GetInt32());
                            break;
                        case "contentType":
                            container.SetContentType(reader.GetString() ?? string.Empty);
                            break;
                        case "data":
                            container.Data = reader.TokenType is JsonTokenType.Null
                                ? null
                                : JsonSerializer.Deserialize<byte[]>(ref reader, options);
                            break;
                        case "hash":
                            container.SetHash(reader.GetString());
                            break;
                        case "providers":
                            providers = JsonSerializer.Deserialize<Dictionary<string, Type>>(ref reader, options) ??
                                        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                            break;
                        default:
                            throw new JsonException($"Property {propertyName} is not supported.");
                    }

                    break;
                }
                default:
#pragma warning disable CA2208
#pragma warning disable MA0015
                    throw new ArgumentOutOfRangeException(nameof(reader.TokenType), "Unexpected token type.");
#pragma warning restore MA0015
#pragma warning restore CA2208
            }
        }

        throw new JsonException("Expected EndObject token.");
    }

    public override void Write(Utf8JsonWriter writer, ContentContainer value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("flags", (int)value.Flags);
        writer.WriteString("contentType", value.ContentType);
        writer.WritePropertyName("data");
        JsonSerializer.Serialize(writer, value.Data, options);
        writer.WriteString("hash", value.Hash);
        writer.WritePropertyName("providers");
        JsonSerializer.Serialize(writer, value.GetProvidersDictionary(),
            options); // Assuming GetProvidersDictionary() method exists to retrieve the dictionary
        writer.WriteEndObject();
    }
}
