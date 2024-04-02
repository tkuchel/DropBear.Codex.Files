using Blake3;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DropBear.Codex.Files.Models.FileComponents.MainComponents
{
    /// <summary>
    /// Represents the metadata of a file.
    /// </summary>
    [MessagePackObject]
    public class FileMetadata
    {
        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        [Key(0)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of the file.
        /// </summary>
        [Key(1)]
        public int FileSize { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the file.
        /// </summary>
        [Key(2)]
        public DateTimeOffset FileCreatedDate { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the last modified date of the file.
        /// </summary>
        [Key(3)]
        public DateTimeOffset FileModifiedDate { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the owner of the file.
        /// </summary>
        [Key(4)]
        public string FileOwner { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of content types present in the file.
        /// </summary>
        [Key(5)]
        public Collection<ContentTypeInfo> ContentTypes { get; set; } = [];

        /// <summary>
        /// Gets or sets the dictionary containing content type verification hashes.
        /// </summary>
        [Key(6)]
        public Dictionary<string, string> ContentTypeVerificationHashes { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the dictionary containing custom metadata.
        /// </summary>
        [Key(7)]
        public Dictionary<string, string> CustomMetadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Updates the last modified date to the current time.
        /// </summary>
        public void UpdateModifiedDate() => FileModifiedDate = DateTimeOffset.UtcNow;

        /// <summary>
        /// Updates the file size to the provided value.
        /// </summary>
        /// <param name="size">The new size of the file.</param>
        public void UpdateFileSize(int size) => FileSize = size;

        /// <summary>
        /// Adds a new content type and hash to the ContentTypeVerificationHashes.
        /// </summary>
        /// <param name="contentType">The content type to add.</param>
        /// <param name="content">The content to hash.</param>
        public void AddContentTypeAndHash(ContentTypeInfo contentType, byte[] content)
        {
            var hash = Hasher.Hash(content).ToString();
            ContentTypeVerificationHashes[contentType.TypeName] = hash;
            AddContentType(contentType);
        }

        /// <summary>
        /// Adds or updates custom metadata key-value pairs.
        /// </summary>
        /// <param name="key">The key of the metadata.</param>
        /// <param name="value">The value of the metadata.</param>
        public void AddOrUpdateCustomMetadata(string key, string value)
        {
            CustomMetadata[key] = value;
            UpdateModifiedDate();
        }

        /// <summary>
        /// Removes a custom metadata entry by key.
        /// </summary>
        /// <param name="key">The key of the metadata to remove.</param>
        /// <returns>True if the metadata was successfully removed, otherwise false.</returns>
        public bool RemoveCustomMetadata(string key)
        {
            UpdateModifiedDate();
            return CustomMetadata.Remove(key);
        }

        /// <summary>
        /// Retrieves a custom metadata value by key.
        /// </summary>
        /// <param name="key">The key of the metadata to retrieve.</param>
        /// <returns>The value of the metadata, or an empty string if not found.</returns>
        public string GetCustomMetadata(string key)
        {
            CustomMetadata.TryGetValue(key, out var value);
            return value ?? string.Empty;
        }

        /// <summary>
        /// Adds a content type if it does not already exist in the list.
        /// </summary>
        /// <param name="contentType">The content type to add.</param>
        private void AddContentType(ContentTypeInfo contentType)
        {
            if (ContentTypes.All(ct => ct.TypeName != contentType.TypeName)) ContentTypes.Add(contentType);
            UpdateModifiedDate();
        }

        /// <summary>
        /// Removes a content type from the list by its type name.
        /// </summary>
        /// <param name="typeName">The type name of the content type to remove.</param>
        /// <returns>True if the content type was successfully removed, otherwise false.</returns>
        public bool RemoveContentType(string typeName)
        {
            UpdateModifiedDate();
            var contentType = ContentTypes.FirstOrDefault(ct => ct.TypeName == typeName);
            return contentType is not null && ContentTypes.Remove(contentType);
        }
    }
}
