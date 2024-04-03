using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.MainComponents;

/// <summary>
///     Represents the header of a file, including version and signature details.
/// </summary>
[MessagePackObject]
public class FileHeader
{
    /// <summary>
    ///     Initializes a new instance of the FileHeader class for MessagePack deserialization and default initialization.
    /// </summary>
    [Obsolete("For MessagePack deserialization. Use parameterized constructor for explicit instantiation.", false)]
    public FileHeader()
    {
        Version = new FileVersion();
        Signature = new FileSignature();
    }

    /// <summary>
    ///     Initializes a new instance of the FileHeader class with specified version and signature.
    /// </summary>
    /// <param name="version">The file version.</param>
    /// <param name="signature">The file signature.</param>
    public FileHeader(FileVersion version, FileSignature signature)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version), "FileVersion cannot be null.");
        Signature = signature ?? throw new ArgumentNullException(nameof(signature), "FileSignature cannot be null.");
    }

    /// <summary>
    ///     Gets the version of the file.
    /// </summary>
    [Key(0)]
    public FileVersion Version { get; private set; }

    /// <summary>
    ///     Gets the signature of the file.
    /// </summary>
    [Key(1)]
    public FileSignature Signature { get; private set; }

    /// <summary>
    ///     Updates the version of the file.
    ///     This method creates a new FileVersion instance to ensure immutability.
    /// </summary>
    /// <param name="major">The major version number.</param>
    /// <param name="minor">The minor version number.</param>
    /// <param name="build">The build number.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void UpdateVersion(int major, int minor, int build)
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major), "Major version number cannot be negative.");

        if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor), "Minor version number cannot be negative.");

        if (build < 0) throw new ArgumentOutOfRangeException(nameof(build), "Build number cannot be negative.");

        Version = new FileVersion(major, minor, build);
    }
}
