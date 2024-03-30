using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents;

/// <summary>
///     Represents the header of a file.
/// </summary>
[MessagePackObject]
public class FileHeader : IFileHeader
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FileHeader" /> class.
    /// </summary>
    [SerializationConstructor]
    public FileHeader()
    {
        // Encode the Year, Month, and Day as bytes. Assuming the Year might need more than one byte.
        var yearBytes = BitConverter.GetBytes((ushort)Version.Major);
        var monthBytes = new[] { (byte)Version.Minor }; // Assuming Minor represents the month
        var dayBytes = new[] { (byte)Version.Build }; // Assuming Build represents the day

        // Ensure bytes are in big-endian order if necessary
        if (BitConverter.IsLittleEndian) Array.Reverse(yearBytes);

        var signatureBytes = new byte[]
        {
            // ASCII for "DBCF"
            0x44, 0x42, 0x43, 0x46,
            // Year, Month, Day in bytes
            yearBytes[0], yearBytes[1], monthBytes[0], dayBytes[0],
            // Random unique sequence
            0xBA, 0xAD, 0xF0, 0x0D
        };

        FileSignature = new FileSignature(signatureBytes, "application/dropbearfile", ".dbf");
    }

    /// <summary>
    ///     Gets the version of the file header.
    /// </summary>
    [Key(0)]
    public FileHeaderVersion Version { get; } = new(2024, 3, 1);

    /// <summary>
    ///     Gets the signature of the file header.
    /// </summary>
    [Key(1)]
    public FileSignature FileSignature { get; }
}
