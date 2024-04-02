using Blake3;
using K4os.Compression.LZ4.Streams;
using MessagePack;
using System;
using System.IO;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents
{
    /// <summary>
    /// Represents a container for file content.
    /// </summary>
    [MessagePackObject]
    public class ContentContainer
    {
        // Default constructor for MessagePack
        [Obsolete("For MessagePack", false)]
        public ContentContainer()
        {
            Name = string.Empty;
            ContentType = new ContentTypeInfo();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentContainer"/> class with the specified name, content, content type, and compression flag.
        /// </summary>
        /// <param name="name">The name of the content container.</param>
        /// <param name="content">The content of the container.</param>
        /// <param name="contentType">The type of content.</param>
        /// <param name="compress">Specifies whether to compress the content.</param>
        public ContentContainer(string name, byte[] content, ContentTypeInfo contentType, bool compress = false)
        {
            Name = name;
            ContentType = contentType;
            SetContent(content, compress);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentContainer"/> class with the specified type, name, content, and compression flag.
        /// </summary>
        /// <param name="type">The type of content.</param>
        /// <param name="name">The name of the content container.</param>
        /// <param name="content">The content of the container.</param>
        /// <param name="compress">Specifies whether to compress the content.</param>
        public ContentContainer(Type type, string name, byte[] content, bool compress = false)
        {
            Name = name;
            SetContent(content, compress);
            ContentType = new ContentTypeInfo(type.Assembly.FullName ?? string.Empty, type.Name,
                type.Namespace ?? string.Empty);
        }

        /// <summary>
        /// Gets or sets the name of the content container.
        /// </summary>
        [Key(0)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the hash of the content container.
        /// </summary>
        [Key(1)]
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content of the container.
        /// </summary>
        [Key(2)]
#pragma warning disable CA1819
        public byte[] Content { get; private set; } = Array.Empty<byte>();
#pragma warning restore CA1819

        /// <summary>
        /// Gets the length of the content.
        /// </summary>
        [Key(3)]
        public int Length => Content?.Length ?? 0;

        /// <summary>
        /// Gets or sets the content type of the container.
        /// </summary>
        [Key(4)]
        public ContentTypeInfo ContentType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the content is compressed.
        /// </summary>
        [Key(5)]
        public bool IsCompressed { get; set; }

        /// <summary>
        /// Updates the hash of the content based on its current state.
        /// </summary>
        /// <returns>The updated hash.</returns>
        public string UpdateContentHash()
        {
            // Compute the hash using the current content (consider compression state)
            var contentToHash = IsCompressed ? DecompressContent(Content) : Content;
            Hash = ComputeHash(contentToHash);

            // Return the updated hash
            return Hash;
        }

        /// <summary>
        /// Sets or updates the content and compresses it if specified.
        /// </summary>
        /// <param name="content">The new content.</param>
        /// <param name="compress">Specifies whether to compress the content.</param>
        public void SetContent(byte[] content, bool compress = false)
        {
            switch (compress)
            {
                case true when !IsCompressed:
                    Content = CompressContent(content);
                    IsCompressed = true;
                    break;
                case false when IsCompressed:
                    Content = DecompressContent(content);
                    IsCompressed = false;
                    break;
                default:
                    Content = content;
                    break;
            }

            Hash = ComputeHash(Content);
        }

        private static byte[] CompressContent(byte[] content)
        {
            using var inputStream = new MemoryStream(content);
            using var compressedStream = new MemoryStream();
            // Wrap the compressedStream with LZ4 compression stream
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, leaveOpen: true))
            {
                inputStream.CopyTo(lz4Stream);
            }

            // It's important to return to the beginning of the stream if you're going to read from it next.
            compressedStream.Position = 0;
            return compressedStream.ToArray();
        }

        internal static byte[] DecompressContent(byte[] compressedContent)
        {
            using var compressedStream = new MemoryStream(compressedContent);
            using var decompressedStream = new MemoryStream();
            // Wrap the compressedStream with LZ4 decompression stream
            using (var lz4Stream = LZ4Stream.Decode(compressedStream, leaveOpen: true))
            {
                lz4Stream.CopyTo(decompressedStream);
            }

            // Again, ensure the stream's position is at the beginning if you intend to read from it right away.
            decompressedStream.Position = 0;
            return decompressedStream.ToArray();
        }

        /// <summary>
        /// Verifies the content hash, recomputes if necessary.
        /// </summary>
        /// <param name="recomputeHash">Specifies whether to recompute the hash.</param>
        /// <returns>True if the hash is valid, otherwise false.</returns>
        public bool VerifyContentHash(bool recomputeHash = false)
        {
            if (!recomputeHash) return Hash == ComputeHash(IsCompressed ? DecompressContent(Content) : Content);
            var currentHash = ComputeHash(Content);
            return Hash == currentHash;
        }

        /// <summary>
        /// Utilizes Blake3 for hash computation.
        /// </summary>
        /// <param name="content">The content to compute the hash for.</param>
        /// <returns>The computed hash.</returns>
        private static string ComputeHash(byte[] content) => Hasher.Hash(content).ToString();
    }
}
