using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;
using System;

namespace DropBear.Codex.Files.Models.FileComponents.MainComponents
{
    /// <summary>
    /// Represents the header of a file.
    /// </summary>
    [MessagePackObject]
    public class FileHeader
    {
        // Default constructor for MessagePack deserialization
        [Obsolete("For MessagePack", false)]
        public FileHeader()
        {
            Version = new FileVersion();
            Signature = new FileSignature();
        }

        // Parameterized constructor for easy instantiation
        /// <summary>
        /// Initializes a new instance of the <see cref="FileHeader"/> class with the specified version and signature.
        /// </summary>
        /// <param name="version">The version of the file.</param>
        /// <param name="signature">The signature of the file.</param>
        public FileHeader(FileVersion version, FileSignature signature)
        {
            Version = version;
            Signature = signature;
        }

        /// <summary>
        /// Gets or sets the version of the file.
        /// </summary>
        [Key(0)]
        public FileVersion Version { get; set; }

        /// <summary>
        /// Gets or sets the signature of the file.
        /// </summary>
        [Key(1)]
        public FileSignature Signature { get; set; }

        /// <summary>
        /// Updates the version of the file.
        /// </summary>
        /// <param name="major">The major version number.</param>
        /// <param name="minor">The minor version number.</param>
        /// <param name="build">The build number.</param>
        public void UpdateVersion(int major, int minor, int build) =>
            Version = new FileVersion { Major = major, Minor = minor, Build = build };
    }
}
