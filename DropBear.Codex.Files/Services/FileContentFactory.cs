using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents;

namespace DropBear.Codex.Files.Services;

public class FileContentFactory : IFileContentFactory
{
    public IFileContent Create()
    {
        // Assuming FileContent is your IFileContent implementation
        return new FileContent();
    }
}
