using DropBear.Codex.Utilities.Helpers;
using ServiceStack.Text;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents
{
#pragma warning disable MA0048
    /// <summary>
    /// Represents a container for generic typed content.
    /// </summary>
    /// <typeparam name="T">The type of content.</typeparam>
    public class ContentContainer<T> : ContentContainer
    {
#pragma warning restore MA0048

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentContainer{T}"/> class with the specified name, content object, and compression flag.
        /// </summary>
        /// <param name="name">The name of the content container.</param>
        /// <param name="contentObject">The content object to store.</param>
        /// <param name="compress">Specifies whether to compress the content.</param>
        public ContentContainer(string name, T contentObject, bool compress = false)
            : base(name, SerializeToByteArray(contentObject),
                new ContentTypeInfo(typeof(T).Assembly.FullName ?? string.Empty, typeof(T).Name,
                    typeof(T).Namespace ?? string.Empty), compress)
        {
        }

        /// <summary>
        /// Serializes the content object to a byte array.
        /// </summary>
        /// <param name="contentObject">The content object to serialize.</param>
        /// <returns>The serialized content as a byte array.</returns>
        private static byte[] SerializeToByteArray(T contentObject) =>
            JsonSerializer.SerializeToString(contentObject).GetBytes();
    }
}
