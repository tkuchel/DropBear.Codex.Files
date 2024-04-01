using DropBear.Codex.Utilities.Helpers;
using ServiceStack.Text;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

#pragma warning disable MA0048
public class ContentContainer<T> : ContentContainer
#pragma warning restore MA0048
{
    // Constructor for object serialization with auto-generated ContentTypeInfo
    public ContentContainer(string name, T contentObject, bool compress = false)
        : base(name, SerializeToByteArray(contentObject),
            new ContentTypeInfo(typeof(T).Assembly.FullName ?? string.Empty, typeof(T).Name,
                typeof(T).Namespace ?? string.Empty), compress)
    {
    }

    // Helper method for serialization
    private static byte[] SerializeToByteArray(T contentObject) =>
        JsonSerializer.SerializeToString(contentObject).GetBytes();
}
