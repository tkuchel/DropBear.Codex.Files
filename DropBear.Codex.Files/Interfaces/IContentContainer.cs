using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Interfaces;

/// <summary>
///     Represents the operations for managing and accessing the contents of a content container.
/// </summary>
[MessagePack.Union(0, typeof(ContentContainer))]
public interface IContentContainer
{
    /// <summary>
    ///     Gets or sets the binary data of the content.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when attempting to set the data to null.</exception>
    byte[] GetData();

    /// <summary>
    ///     Gets or sets the binary data of the content.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when attempting to set the data to null.</exception>
    void SetData(byte[] value);

    /// <summary>
    ///     Gets the information describing the type of the content.
    /// </summary>
    ContentTypeInfo ContentType { get; }

    /// <summary>
    ///     Gets the total length of the content data in bytes.
    /// </summary>
    long ContentLength { get; }

    /// <summary>
    ///     Gets the verification hash for the content data. This hash is used to verify
    ///     the integrity of the content data.
    /// </summary>
    string VerificationHash { get; }
}
