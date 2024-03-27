using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileContent
{
    IReadOnlyList<IContentContainer> Contents { get; }
    void AddContent(IContentContainer content);
    void RemoveContent(IContentContainer content);
    void ClearContents();
}
