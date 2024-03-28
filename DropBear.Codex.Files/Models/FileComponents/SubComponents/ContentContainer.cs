using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Utilities.Hashing;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

/// <summary>
///     Represents a content container.
/// </summary>
[MessagePackObject]
public class ContentContainer : IContentContainer
{
    private readonly BlakePasswordHasher _blakePasswordHasher = new();
    private byte[] _data;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ContentContainer" /> class.
    /// </summary>
    /// <param name="data">The content data.</param>
    /// <param name="contentType">The content type information.</param>
    public ContentContainer(byte[] data, ContentTypeInfo contentType)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        VerificationHash = _blakePasswordHasher.Base64EncodedHash(_data).Value;
    }

    /// <summary>
    ///     Gets or sets the content data.
    /// </summary>
    [Key(0)]
#pragma warning disable CA1819
    public byte[] Data
#pragma warning restore CA1819
    {
        get => _data;
        private set
        {
            ArgumentNullException.ThrowIfNull(value);
            _data = value;
            VerificationHash = _blakePasswordHasher.Base64EncodedHash(_data).Value; // Consider error handling here
        }
    }

    /// <summary>
    ///     Gets the content type information.
    /// </summary>
    [Key(1)]
    public ContentTypeInfo ContentType { get; }

    /// <summary>
    ///     Gets the content length.
    /// </summary>
    [IgnoreMember]
    public long ContentLength => Data.Length;

    /// <summary>
    ///     Gets or sets the verification hash of the content.
    /// </summary>
    [Key(2)]
    public string VerificationHash { get; private set; }
}
