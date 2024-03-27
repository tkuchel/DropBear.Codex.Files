using System.Collections.ObjectModel;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.Bases;

namespace DropBear.Codex.Files.Models.FileComponents;

/// <summary>
///     Represents the content of a custom file type as a read-only collection of bytes.
/// </summary>
public class FileContent : FileComponentBase, IFileContent
{
    private ReadOnlyCollection<byte> _content = new(Array.Empty<byte>());

    /// <summary>
    ///     Gets the file content as a read-only list of bytes.
    /// </summary>
    public IReadOnlyList<byte> Content => _content;

    /// <summary>
    ///     Sets the file content from a sequence of bytes.
    /// </summary>
    /// <param name="content">The content to set.</param>
    /// <exception cref="ArgumentNullException">Thrown if content is null.</exception>
    public void SetContent(IEnumerable<byte> content)
    {
        if (content is null) throw new ArgumentNullException(nameof(content), "Content cannot be null.");
        _content = new ReadOnlyCollection<byte>(content.ToList());
    }
}
