using FastRsync.Delta;
using FastRsync.Diagnostics;
using FastRsync.Signature;

namespace DropBear.Codex.Files.Models;

public class FileVersion
{
    public FileVersion(string versionLabel, DateTimeOffset versionDate, string currentFilePath, string newFilePath,
        string deltaFilePath, string signatureFilePath, string baseFilePath)
    {
        VersionLabel = versionLabel ?? throw new ArgumentNullException(nameof(versionLabel));
        VersionDate = versionDate;
        CurrentFilePath = currentFilePath;
        NewFilePath = newFilePath;
        DeltaFilePath = deltaFilePath;
        SignatureFilePath = signatureFilePath;
        BaseFilePath = baseFilePath;
    }

    public string BaseFilePath { get; }
    public string CurrentFilePath { get; }
    public string NewFilePath { get; }
    public string DeltaFilePath { get; }
    public string SignatureFilePath { get; }

    public DateTimeOffset VersionDate { get; set; }

    public string VersionLabel { get; set; }

    public void CreateDelta()
    {
        var signatureBuilder = new SignatureBuilder();
        using (var basisStream = new FileStream(CurrentFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var signatureStream =
               new FileStream(SignatureFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
        }

        using (var newFileStream = new FileStream(NewFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var deltaFileStream = new FileStream(DeltaFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var signatureFileStream =
               new FileStream(SignatureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var signatureReader = new SignatureReader(signatureFileStream, new ConsoleProgressReporter());
            var deltaBuilder = new DeltaBuilder();
            deltaBuilder.BuildDelta(newFileStream, signatureReader, new BinaryDeltaWriter(deltaFileStream));
        }
    }

    public void ApplyDelta()
    {
        using var basisFileStream = new FileStream(CurrentFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var deltaFileStream = new FileStream(DeltaFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var targetFileStream = new FileStream(NewFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        var deltaReader = new BinaryDeltaReader(deltaFileStream, new ConsoleProgressReporter());
        var deltaApplier = new DeltaApplier();

        deltaApplier.Apply(basisFileStream, deltaReader, targetFileStream);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is FileVersion other)
        {
            return VersionLabel == other.VersionLabel &&
                   VersionDate == other.VersionDate &&
                   BaseFilePath == other.BaseFilePath &&
                   CurrentFilePath == other.CurrentFilePath &&
                   NewFilePath == other.NewFilePath &&
                   DeltaFilePath == other.DeltaFilePath &&
                   SignatureFilePath == other.SignatureFilePath;
        }
        return false;
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(VersionLabel);
        hash.Add(VersionDate);
        hash.Add(BaseFilePath);
        hash.Add(CurrentFilePath);
        hash.Add(NewFilePath);
        hash.Add(DeltaFilePath);
        hash.Add(SignatureFilePath);
        return hash.ToHashCode();
    }


}
