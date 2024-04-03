using DropBear.Codex.Utilities.Helpers;
using ServiceStack;
using ServiceStack.Text;

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
            return JsonSerializer.SerializeToString(contentObject).GetBytes();
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
            return JsonSerializer.DeserializeFromString<T>(Content.FromUtf8Bytes());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Deserialization failed.", ex);
        }
    }
}
