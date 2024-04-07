using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Interfaces;

public interface IContentContainer
{
    string Name { get; }
    string Hash { get; }
    byte[] Content();
    int Length { get; }
    ContentTypeInfo ContentType { get; }
    bool IsCompressed { get; }
    bool VerifyContentHash(bool recomputeHash = false);
}
