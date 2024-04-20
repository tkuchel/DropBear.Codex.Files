namespace DropBear.Codex.Files.Interfaces;

public interface IFileVersion
{
    string VersionLabel { get; }
    DateTime VersionDate { get; }
    string DeltaFilePath { get; } // Path to the delta file
    string SignatureFilePath { get; } // Path to the signature file
    void CreateDelta(string basisFilePath, string newPath);
    void ApplyDelta(string basisFilePath, string targetPath);
}
