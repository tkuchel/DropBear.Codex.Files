using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents
{
    /// <summary>
    /// Represents the version of a file.
    /// </summary>
    [MessagePackObject]
    public class FileVersion : IEquatable<FileVersion>, IComparable<FileVersion>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileVersion"/> class with default values.
        /// </summary>
        public FileVersion()
        {
            // Set default values to the current year and month
            Major = DateTime.Today.Year;
            Minor = DateTime.Today.Month;
            Build = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileVersion"/> class with the specified build number.
        /// </summary>
        /// <param name="buildNumber">The build number.</param>
        public FileVersion(int buildNumber)
        {
            // Set default values to the current year and month, and the provided build number
            Major = DateTime.Today.Year;
            Minor = DateTime.Today.Month;
            Build = buildNumber;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileVersion"/> class with the specified major, minor, and build numbers.
        /// </summary>
        /// <param name="major">The major version number.</param>
        /// <param name="minor">The minor version number.</param>
        /// <param name="build">The build version number.</param>
        public FileVersion(int major, int minor, int build)
        {
            Major = major;
            Minor = minor;
            Build = build;
        }

        /// <summary>
        /// Gets or sets the major version number.
        /// </summary>
        [Key(0)]
        public int Major { get; init; }

        /// <summary>
        /// Gets or sets the minor version number.
        /// </summary>
        [Key(1)]
        public int Minor { get; init; }

        /// <summary>
        /// Gets or sets the build version number.
        /// </summary>
        [Key(2)]
        public int Build { get; init; }

        /// <inheritdoc/>
        public int CompareTo(FileVersion? other)
        {
            if (other == null) return 1;

            // Compare major version
            var majorComparison = Major.CompareTo(other.Major);
            if (majorComparison is not 0) return majorComparison;

            // Compare minor version
            var minorComparison = Minor.CompareTo(other.Minor);
            if (minorComparison is not 0) return minorComparison;

            // Compare build version
            return Build.CompareTo(other.Build);
        }

        /// <inheritdoc/>
        public bool Equals(FileVersion? other) =>
            other != null &&
            Major == other.Major &&
            Minor == other.Minor &&
            Build == other.Build;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as FileVersion);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Major, Minor, Build);

        // Define comparison operators

        /// <inheritdoc/>
        public static bool operator ==(FileVersion? left, FileVersion? right) =>
            EqualityComparer<FileVersion>.Default.Equals(left, right);

        /// <inheritdoc/>
        public static bool operator !=(FileVersion? left, FileVersion? right) => !(left == right);

        /// <inheritdoc/>
        public static bool operator <(FileVersion? left, FileVersion? right) =>
            left is null ? right is not null : left.CompareTo(right) < 0;

        /// <inheritdoc/>
        public static bool operator <=(FileVersion? left, FileVersion? right) => left is null || left.CompareTo(right) <= 0;

        /// <inheritdoc/>
        public static bool operator >(FileVersion? left, FileVersion? right) =>
            left is not null && left.CompareTo(right) > 0;

        /// <inheritdoc/>
        public static bool operator >=(FileVersion? left, FileVersion? right) =>
            left is not null && left.CompareTo(right) >= 0;
    }
}
