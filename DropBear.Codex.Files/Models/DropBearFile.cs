using DropBear.Codex.Files.Models.FileComponents.MainComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;

namespace DropBear.Codex.Files.Models;

[MessagePackObject]
public class DropBearFile
{
    // Default constructor for MessagePack and empty file creation
    public DropBearFile()
    {
        // Initialize with default values
        Header = new FileHeader();
        Metadata = new FileMetadata();
        Content = new FileContent();
        CompressContent = false; // Default to not compressing content
    }

    // Constructor that accepts minimal parameters for file creation
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

    [Key(0)] public FileHeader Header { get; set; }

    [Key(1)] public FileMetadata Metadata { get; set; }

    [Key(2)] public FileContent Content { get; set; }

    [Key(3)] public bool CompressContent { get; set; }

    // Method to add a ContentContainer to the file
    public void AddContent(ContentContainer content)
    {
        Content.AddContent(content);
        // Update metadata to reflect new content addition
        Metadata.FileSize += content.Length;
        Metadata.UpdateModifiedDate();
    }
    
    // Method to add content to the file
    public void AddContent(byte[] contentData, ContentTypeInfo contentType, string contentName, bool compress = false)
    {
        var contentContainer = new ContentContainer(contentName, contentData, contentType, compress || CompressContent);
        Content.AddContent(contentContainer);
        // Update metadata to reflect new content addition
        Metadata.FileSize += contentContainer.Length;
        Metadata.UpdateModifiedDate();
    }

    // Method to update the compression state of the entire file
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

    public bool VerifyDropBearFileIntegrity() =>
        // Verify File Metadata
        VerifyMetadata() &&
        // Update and Verify Content Hashes
        UpdateAndVerifyContentHashes();

    private bool VerifyMetadata()
    {
        // Verify that the Metadata.FileSize matches the sum of the Content lengths
        var totalContentSize = Content.Contents.Sum(content => content.Length);
        return Metadata.FileSize == totalContentSize;
    }

    private bool UpdateAndVerifyContentHashes()
    {
        foreach (var content in Content.Contents)
        {
            // Update hash for each content
            var updatedHash = content.UpdateContentHash(); // Assumes existence of a method to update hash

            // Verify the hash is correct (e.g., matches a stored or expected value)
            if (!content.VerifyContentHash()) return false; // Hash verification failed for content

            // Optionally, update ContentTypeVerificationHashes in Metadata if needed
            Metadata.ContentTypeVerificationHashes[content.ContentType.TypeName] = updatedHash;
        }

        return true; // All content hashes are updated and verified
    }
}
