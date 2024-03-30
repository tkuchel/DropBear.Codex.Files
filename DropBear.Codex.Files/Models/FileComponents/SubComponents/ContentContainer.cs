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
    private readonly Blake3HashingService _blake3HashingService = new();

    [Key(0)] private byte[] _data;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ContentContainer" /> class.
    /// </summary>
    /// <param name="data">The content data.</param>
    /// <param name="contentType">The content type information.</param>
    [SerializationConstructor]
    public ContentContainer(byte[] data, ContentTypeInfo contentType)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
    }

    /// <summary>
    ///     Gets or sets the content data.
    /// </summary>
    public byte[] GetData() => _data;

    /// <summary>
    ///     Gets or sets the content data.
    /// </summary>
    public void SetData(byte[] value)
    {
        _data = value ?? throw new ArgumentNullException(nameof(value), "Data cannot be null.");
        VerificationHash =
            _blake3HashingService.EncodeToBase64Hash(_data).Value; // Recalculate hash on data update.
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
    public long ContentLength => GetData().Length;

    /// <summary>
    ///     Gets the verification hash of the content.
    /// </summary>
    [Key(2)]
    public string VerificationHash { get; private set; } = string.Empty;
}
