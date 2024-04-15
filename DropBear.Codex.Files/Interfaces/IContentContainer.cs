namespace DropBear.Codex.Files.Interfaces;

public interface IContentContainer
{
    string ContentType { get; }
    byte[] Data { get; }
    string Hash { get; }

    void SetData(byte[] data);
    void ApplyStrategy(IContentStrategy strategy);
    void ComputeHash();
    bool VerifyHash(string expectedHash);
}
