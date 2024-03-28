using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Interfaces;

/// <summary>
///     Interface representing a content container.
/// </summary>
public interface IContentContainer
{
    /// <summary>
    ///     Gets the data stored in the content container.
    /// </summary>
#pragma warning disable CA1819
    byte[] Data { get; }
#pragma warning restore CA1819

    /// <summary>
    ///     Gets the content type information of the content container.
    /// </summary>
    ContentTypeInfo ContentType { get; }

    /// <summary>
    ///     Gets the length of the content data.
    /// </summary>
    long ContentLength => Data.Length;

    /// <summary>
    ///     Gets the verification hash of the content container.
    /// </summary>
    string VerificationHash { get; }
}
