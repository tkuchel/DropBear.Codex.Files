using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileHeader
{
    Version Version { get;  }
    FileSignature FileSignature { get; } 
}
