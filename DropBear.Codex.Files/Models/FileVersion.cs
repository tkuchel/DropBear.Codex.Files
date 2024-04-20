using DropBear.Codex.Files.Interfaces;
using FastRsync.Delta;
using FastRsync.Diagnostics;
using FastRsync.Signature;

namespace DropBear.Codex.Files.Models;

public class FileVersion : IFileVersion
{
    private readonly string baseFilePath;

    public FileVersion(string versionLabel, DateTime versionDate, string baseFilePath)
    {
        VersionLabel = versionLabel ?? throw new ArgumentNullException(nameof(versionLabel));
        VersionDate = versionDate;
        this.baseFilePath = baseFilePath ?? throw new ArgumentNullException(nameof(baseFilePath));
    }

    public string VersionLabel { get; }
    public DateTime VersionDate { get; }

    public string DeltaFilePath => $"{baseFilePath}.{VersionLabel}.delta";
    public string SignatureFilePath => $"{baseFilePath}.{VersionLabel}.sig";

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
            var signatureReader = new SignatureReader(signatureFileStream, new ConsoleProgressReporter());
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
