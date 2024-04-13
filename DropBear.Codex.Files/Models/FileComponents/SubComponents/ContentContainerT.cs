using System.Text;
using DropBear.Codex.Utilities.Helpers;
using Newtonsoft.Json;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

#pragma warning disable MA0048
public class ContentContainer<T> : ContentContainer
#pragma warning restore MA0048
{
    public ContentContainer(string name, T contentObject, bool compress = false)
        : base(name, SerializeToByteArray(contentObject),
            new ContentTypeInfo(typeof(T).Assembly.FullName ?? string.Empty, typeof(T).Name,
                typeof(T).Namespace ?? string.Empty),
            compress)
    {
    }

    private static byte[] SerializeToByteArray(T contentObject)
    {
        try
        {
            return JsonConvert.SerializeObject(contentObject).GetBytes();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Serialization failed.", ex);
        }
    }

    public T DeserializeContent()
    {
        try
        {
            // Assuming Content is uncompressed; if compressed, decompression should occur before deserialization
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(Content)) ??
                   throw new InvalidOperationException("Deserialization failed.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Deserialization failed.", ex);
        }
    }
}
