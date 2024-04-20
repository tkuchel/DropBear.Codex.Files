using DropBear.Codex.Files.Interfaces;
using FastRsync.Delta;
using FastRsync.Diagnostics;
using FastRsync.Signature;

namespace DropBear.Codex.Files.Models;

public class FileVersion : IFileVersion
{
    public FileVersion(string versionLabel, DateTime versionDate, string deltaFilePath, string signatureFilePath)
    {
        VersionLabel = versionLabel ?? throw new ArgumentNullException(nameof(versionLabel));
        VersionDate = versionDate;
        DeltaFilePath = deltaFilePath ?? throw new ArgumentNullException(nameof(deltaFilePath));
        SignatureFilePath = signatureFilePath ?? throw new ArgumentNullException(nameof(signatureFilePath));
    }

    public string VersionLabel { get; private set; }
    public DateTime VersionDate { get; private set; }
    public string DeltaFilePath { get; } // Path to the delta file
    public string SignatureFilePath { get; } // Path to the signature file

    // Correctly create a signature and then a delta
    public void CreateDelta(string basisFilePath, string newPath)
    {
        // Create the signature file first
        var signatureBuilder = new SignatureBuilder();
        using (var basisStream = new FileStream(basisFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var signatureStream =
               new FileStream(SignatureFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
        }

        // Use the signature file to create a delta
        using (var newFileStream = new FileStream(newPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var deltaFileStream = new FileStream(DeltaFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var signatureFileStream =
               new FileStream(SignatureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var signatureReader = new SignatureReader(signatureFileStream,new ConsoleProgressReporter());
            var deltaBuilder = new DeltaBuilder();
            deltaBuilder.BuildDelta(newFileStream, signatureReader, new BinaryDeltaWriter(deltaFileStream));
        }
    }

    public void ApplyDelta(string basisFilePath, string targetPath)
    {
        using var basisFileStream = new FileStream(basisFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var deltaFileStream = new FileStream(DeltaFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var targetFileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        var deltaReader = new BinaryDeltaReader(deltaFileStream, new ConsoleProgressReporter());
        var deltaApplier = new DeltaApplier();

        deltaApplier.Apply(basisFileStream, deltaReader, targetFileStream);
    }
}
