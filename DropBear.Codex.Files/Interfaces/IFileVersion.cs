namespace DropBear.Codex.Files.Interfaces;

public interface IFileVersion
{
    string VersionLabel { get; }
    DateTime VersionDate { get; }
    void Compare(IFileVersion otherVersion);
    void Rollback();
    void Update(IFileVersion newVersion);
}
