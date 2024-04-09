using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Interfaces;

public interface IContentContainer
{
    string Name { get; }
    string Hash { get; }
#pragma warning disable CA1819
    byte[] Content { get; }
#pragma warning restore CA1819
    int Length { get; }
    ContentTypeInfo ContentType { get; }
    bool IsCompressed { get; }
    bool VerifyContentHash(bool recomputeHash = false);
}
