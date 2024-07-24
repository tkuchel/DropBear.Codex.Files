#region

using FastRsync.Delta;
using FastRsync.Diagnostics;
using FastRsync.Signature;

#endregion

namespace DropBear.Codex.Files.Utils;

public static class DeltaGenerator
{
#pragma warning disable MA0004
    public static async Task GenerateSignatureAsync(string basisFilePath, string signatureFilePath)
    {
        await using var basisStream = new FileStream(basisFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var signatureStream =
            new FileStream(signatureFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await GenerateSignatureAsync(basisStream, signatureStream);
    }

    public static async Task GenerateDeltaAsync(string newFilePath, string signatureFilePath, string deltaFilePath)
    {
        await using var newFileStream = new FileStream(newFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var signatureStream =
            new FileStream(signatureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var deltaStream = new FileStream(deltaFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await GenerateDeltaAsync(newFileStream, signatureStream, deltaStream);
    }

    public static async Task ApplyDeltaAsync(string basisFilePath, string deltaFilePath, string newFilePath)
    {
        await using var basisStream = new FileStream(basisFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var deltaStream = new FileStream(deltaFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var newFileStream = new FileStream(newFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await ApplyDeltaAsync(basisStream, deltaStream, newFileStream);
    }

    // Overloaded methods to handle streams directly
    public static async Task GenerateSignatureAsync(Stream basisStream, Stream signatureStream)
    {
        var signatureBuilder = new SignatureBuilder();
        await signatureBuilder.BuildAsync(basisStream, new SignatureWriter(signatureStream));
    }

    public static async Task GenerateDeltaAsync(Stream newFileStream, Stream signatureStream, Stream deltaStream)
    {
        var deltaBuilder = new DeltaBuilder();
        await deltaBuilder.BuildDeltaAsync(newFileStream,
            new SignatureReader(signatureStream, new ConsoleProgressReporter()), new BinaryDeltaWriter(deltaStream));
    }

    public static async Task ApplyDeltaAsync(Stream basisStream, Stream deltaStream, Stream newFileStream)
    {
        var deltaApplier = new DeltaApplier { SkipHashCheck = true };
        await deltaApplier.ApplyAsync(basisStream, new BinaryDeltaReader(deltaStream, new ConsoleProgressReporter()),
            newFileStream);
    }
#pragma warning restore MA0004
}
