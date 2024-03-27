namespace DropBear.Codex.Files.Interfaces;

public interface IFileContent
{
    IReadOnlyList<byte> Content { get; }
    void SetContent(IEnumerable<byte> content);
}
