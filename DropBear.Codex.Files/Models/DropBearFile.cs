using System.IO.Compression;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.Bases;
using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using static System.DateTimeOffset;

namespace DropBear.Codex.Files.Models;

public class DropBearFile : FileBase
{
    public DropBearFile()
    {
        // Initialize FileHeader with unique signature and version info
        Header = new FileHeader(); // Assuming FileHeader implements IFileHeader

        // Example MetaData initialization
        MetaData = new FileMetaData(
            createdDate: Now,
            lastModifiedDate: Now,
            "Author Name",
            new List<ContentTypeInfo>
            {
                new() { AssemblyName = "ExampleAssembly", TypeName = "ExampleType", Namespace = "ExampleNamespace" },
            });

        // Example CompressionSettings initialization
        CompressionSettings = new CompressionSettings
        {
            IsCompressed = true, CompressionLevel = CompressionLevel.Optimal
        };

        // Initialize FileContent with example data
        Content = new FileContent();
        Content.SetContent(new byte[] { 0x01, 0x02, 0x03, 0x04 }); // Sample content bytes
    }

    public IFileHeader Header { get; private set; }
    public IFileMetaData MetaData { get; private set; }
    public ICompressionSettings CompressionSettings { get; private set; }
    public IFileContent Content { get; }
}
