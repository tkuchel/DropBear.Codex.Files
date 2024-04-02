using Blake3;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DropBear.Codex.Files.Exceptions;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.MainComponents
{
    /// <summary>
    /// Represents the content of a file.
    /// </summary>
    [MessagePackObject]
    public class FileContent
    {
        /// <summary>
        /// Gets or sets the list of content containers.
        /// </summary>
        [Key(0)]
        public Collection<ContentContainer> Contents { get; set; } = [];

        /// <summary>
        /// Adds content to the file content.
        /// </summary>
        /// <param name="content">The content to add.</param>
        public void AddContent(ContentContainer content) => Contents.Add(content);

        /// <summary>
        /// Removes content from the file content.
        /// </summary>
        /// <param name="content">The content to remove.</param>
        public void RemoveContent(ContentContainer content) => Contents.Remove(content);

        /// <summary>
        /// Clears all content from the file content.
        /// </summary>
        public void ClearContent() => Contents.Clear();

        /// <summary>
        /// Verifies the hash of a content container.
        /// </summary>
        /// <param name="content">The content container to verify.</param>
        /// <returns>True if the hash is valid, otherwise false.</returns>
        private static bool VerifyContentHash(ContentContainer content)
        {
            var computedHash = Hasher.Hash(content.Content).ToString();
            return content.Hash == computedHash;
        }

        /// <summary>
        /// Gets all content containers of a specific type.
        /// </summary>
        /// <typeparam name="T">The type of content container to retrieve.</typeparam>
        /// <returns>An enumerable collection of content containers of the specified type.</returns>
        public IEnumerable<ContentContainer> GetAllContents<T>() =>
            Contents.Where(c => c.ContentType.GetContentType() == typeof(T));

        /// <summary>
        /// Gets the content container of a specific type.
        /// </summary>
        /// <typeparam name="T">The type of content container to retrieve.</typeparam>
        /// <returns>The content container of the specified type.</returns>
        public ContentContainer GetContent<T>() =>
            GetAllContents<T>().FirstOrDefault() ?? throw new FileContentNotFoundException();

        /// <summary>
        /// Gets the content container of any type.
        /// </summary>
        /// <returns>The content container of any type.</returns>
        public ContentContainer GetContent() =>
            Contents.FirstOrDefault() ?? throw new FileContentNotFoundException();

        /// <summary>
        /// Gets the raw content of a specific type.
        /// </summary>
        /// <typeparam name="T">The type of content to retrieve.</typeparam>
        /// <returns>The raw content of the specified type.</returns>
        public byte[] GetRawContent<T>()
        {
            var container = GetContent<T>();
            return container?.Content ?? throw new FileContentNotFoundException();
        }

        /// <summary>
        /// Gets the raw content of any type.
        /// </summary>
        /// <returns>The raw content of any type.</returns>
        public byte[] GetRawContent()
        {
            var container = GetContent();
            return container?.Content ?? throw new FileContentNotFoundException();
        }

        /// <summary>
        /// Finds a content container of a specific type.
        /// </summary>
        /// <typeparam name="T">The type of content container to find.</typeparam>
        /// <returns>The content container of the specified type.</returns>
        public ContentContainer FindContainerForType<T>() =>
            Contents.FirstOrDefault(c => c.ContentType.GetContentType() == typeof(T)) ?? throw new FileContentNotFoundException();

        /// <summary>
        /// Finds a content container of a specific type and name.
        /// </summary>
        /// <typeparam name="T">The type of content container to find.</typeparam>
        /// <param name="name">The name of the content container.</param>
        /// <returns>The content container of the specified type and name.</returns>
        public ContentContainer FindContainerForTypeAndName<T>(string name) =>
            Contents.FirstOrDefault(c => c.ContentType.GetContentType() == typeof(T) && c.Name == name) ??
            throw new FileContentNotFoundException();

        /// <summary>
        /// Finds a content container of a specific type, name, and hash.
        /// </summary>
        /// <typeparam name="T">The type of content container to find.</typeparam>
        /// <param name="name">The name of the content container.</param>
        /// <param name="hash">The hash of the content container.</param>
        /// <returns>The content container of the specified type, name, and hash.</returns>
        public ContentContainer FindContainerForTypeAndNameAndHash<T>(string name, string hash) =>
            Contents.FirstOrDefault(c => c.ContentType.GetContentType() == typeof(T) && c.Name == name && c.Hash == hash) ??
            throw new FileContentNotFoundException();

        // Other search methods...

        /// <summary>
        /// Verifies the hashes of all content containers.
        /// </summary>
        /// <returns>True if all hashes are valid, otherwise false.</returns>
        public bool VerifyAllContentHashes() => Contents.All(VerifyContentHash);
    }
}
