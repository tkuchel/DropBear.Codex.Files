using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DropBear.Codex.Files.Converters;

public class TypeConverter : JsonConverter<Type>
{
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var typeName = reader.GetString();
        return typeName is null ? null : Type.GetType(typeName, AssemblyResolver, typeResolver: null);

        static Assembly AssemblyResolver(AssemblyName assemblyName)
        {
            // Attempt to load the specified assembly
            try
            {
                return Assembly.Load(assemblyName);
            }
            catch
            {
                // Handle or log the error as needed
                return null!;
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.AssemblyQualifiedName);
}