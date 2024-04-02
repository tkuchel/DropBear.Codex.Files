using DropBear.Codex.Files.Exceptions;
using DropBear.Codex.Files.Models.FileComponents.MainComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;

namespace DropBear.Codex.Files.Models
{
    /// <summary>
    /// Represents a DropBear file, containing header, metadata, and content.
    /// </summary>
    [MessagePackObject]
    public class DropBearFile
    {
        /// <summary>
        /// Default constructor for MessagePack and empty file creation.
        /// </summary>
        [Obsolete("For MessagePack", false)]
        public DropBearFile()
        {
            // Initialize with default values
            Header = new FileHeader();
            Metadata = new FileMetadata();
            Content = new FileContent();
            CompressContent = false; // Default to not compressing content
        }

        /// <summary>
        /// Constructor that accepts minimal parameters for file creation.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="fileOwner">The owner of the file.</param>
        /// <param name="compressContent">Whether to compress the content.</param>
        public DropBearFile(string fileName, string fileOwner, bool compressContent = false)
        {
            Header = new FileHeader(new FileVersion(), new FileSignature());
            Metadata = new FileMetadata
            {
                FileName = fileName,
                FileOwner = fileOwner,
                FileCreatedDate = DateTimeOffset.UtcNow,
                FileModifiedDate = DateTimeOffset.UtcNow
            };
            Content = new FileContent();
            CompressContent = compressContent;
        }

        /// <summary>
        /// Gets or sets the header of the DropBear file.
        /// </summary>
        [Key(0)] public FileHeader? Header { get; set; }

        /// <summary>
        /// Gets or sets the metadata of the DropBear file.
        /// </summary>
        [Key(1)] public FileMetadata Metadata { get; set; }

        /// <summary>
        /// Gets or sets the content of the DropBear file.
        /// </summary>
        [Key(2)] public FileContent Content { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the content of the DropBear file should be compressed.
        /// </summary>
        [Key(3)] public bool CompressContent { get; set; }

        /// <summary>
        /// Method to add a <see cref="ContentContainer"/> to the file.
        /// </summary>
        /// <param name="content">The content to add.</param>
        public void AddContent(ContentContainer content)
        {
            // Add content to the file
            Content.AddContent(content);

            // Update metadata to reflect new content addition
            Metadata.FileSize += content.Length;
            Metadata.AddContentTypeAndHash(content.ContentType, content.Content);
            Metadata.UpdateModifiedDate();
        }

        /// <summary>
        /// Method to add content to the file.
        /// </summary>
        /// <param name="contentData">The content data to add.</param>
        /// <param name="contentType">The content type information.</param>
        /// <param name="contentName">The name of the content.</param>
        /// <param name="compress">Whether to compress the content.</param>
        public void AddContent(byte[] contentData, ContentTypeInfo contentType, string contentName, bool compress = false)
        {
            var contentContainer = new ContentContainer(contentName, contentData, contentType, compress || CompressContent);
            AddContent(contentContainer);
            
            // Update metadata to reflect new content addition
            Metadata.FileSize += contentData.Length;
            Metadata.AddContentTypeAndHash(contentType, contentData);
            Metadata.UpdateModifiedDate();
        }

        /// <summary>
        /// Method to update the compression state of the entire file.
        /// </summary>
        /// <param name="compress">Whether to compress the content.</param>
        public void UpdateCompressionState(bool compress)
        {
            CompressContent = compress;

            // Iterate over all content containers to update their compression state
            foreach (var content in Content.Contents)
            {
                // Check if the current compression state matches the desired state
                if (content.IsCompressed == compress) continue;

                // Get the current content, compressed or decompressed as needed
                var currentContent = content.IsCompressed
                    ? ContentContainer.DecompressContent(content.Content)
                    : content.Content;

                // Set the content again, specifying whether it should be compressed
                content.SetContent(currentContent, compress);
            }
        }

        /// <summary>
        /// Verifies the integrity of the DropBear file.
        /// </summary>
        /// <returns>True if the file is valid, otherwise false.</returns>
        private bool VerifyDropBearFileIntegrity() =>
            // Verify File Metadata
            VerifyMetadata() &&
            //Verify Content Hashes
            VerifyContentHashes();

        // Private methods for integrity verification

        private bool VerifyMetadata()
        {
            // Verify that the Metadata.FileSize matches the sum of the Content lengths
            var totalContentSize = Content.Contents.Sum(content => content.Length);
            return Metadata.FileSize == totalContentSize;
        }
        
        private string GetFileName() => Metadata.FileName;
        
        private string GetExtension() => Header?.Signature.Extension ?? "dbf";
        
        public string GetFileNameWithExtension() => $"{GetFileName()}.{GetExtension()}";

        private bool UpdateAndVerifyContentHashes()
        {
            foreach (var content in Content.Contents)
            {
                // Update hash for each content
                var updatedHash = content.UpdateContentHash();

                // Verify the hash is correct (e.g., matches a stored or expected value)
                if (!content.VerifyContentHash()) return false;

                // Optionally, update ContentTypeVerificationHashes in Metadata if needed
                Metadata.ContentTypeVerificationHashes[content.ContentType.TypeName] = updatedHash;
            }

            return true; // All content hashes are updated and verified
        }
        
        private bool VerifyContentHashes()
        {
            return Content.Contents.All(content => content.VerifyContentHash());
        }

        /// <summary>
        /// Reconstructs a DropBear file using the provided header, metadata, and content.
        /// </summary>
        /// <param name="header">The header of the file.</param>
        /// <param name="metadata">The metadata of the file.</param>
        /// <param name="content">The content of the file.</param>
        /// <returns>The reconstructed DropBear file.</returns>
        public static DropBearFile Reconstruct(FileHeader header, FileMetadata metadata, FileContent content)
        {
            // Initialize a new DropBearFile instance with metadata.
            var dropBearFile = new DropBearFile(metadata.FileName, metadata.FileOwner)
            {
                Header = header,
                Metadata = metadata,
                Content = content
            };

            // Verify the integrity of the reconstructed DropBearFile.
            if (!dropBearFile.VerifyDropBearFileIntegrity())
                throw new IntegrityVerificationFailedException(
                    "The reconstructed DropBearFile failed integrity verification.");

            return dropBearFile;
        }
    }
}
