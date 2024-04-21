using System.Text.Json;
using System.Text.Json.Serialization;

namespace DropBear.Codex.Files.Converters;

public class TypeConverter : JsonConverter<Type>
{
    public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var typeName = reader.GetString();
        return !string.IsNullOrEmpty(typeName)
            ? Type.GetType(typeName)
            : throw new JsonException($"Type not found for {typeName}.");
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.AssemblyQualifiedName);
}
