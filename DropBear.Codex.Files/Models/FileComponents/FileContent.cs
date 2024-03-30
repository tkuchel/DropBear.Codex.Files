using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.Bases;
using MessagePack;
using System;
using System.Collections.Generic;

namespace DropBear.Codex.Files.Models.FileComponents;

/// <summary>
/// Represents the content of a file.
/// </summary>
[MessagePackObject]
public class FileContent : FileComponentBase, IFileContent
{
    [SerializationConstructor]
    public FileContent()
    {
        
    }
    
    // Backing field for the Contents collection is no longer needed explicitly
    
    /// <summary>
    /// This property is used by MessagePack for serialization and deserialization.
    /// It's not intended to be used directly by other parts of your code.
    /// </summary>
    [Key(0)]
    private List<IContentContainer> ContentsSerialization { get; set; } = new();

    /// <summary>
    /// Gets the content containers in the file content.
    /// </summary>
    public IReadOnlyList<IContentContainer> Contents => ContentsSerialization.AsReadOnly();

    /// <summary>
    /// Adds a content container to the file content.
    /// </summary>
    /// <param name="content">The content container to add.</param>
    public void AddContent(IContentContainer content)
    {
        if (content is null) throw new ArgumentNullException(nameof(content), "Content cannot be null.");
        ContentsSerialization.Add(content);
    }

    /// <summary>
    /// Removes a content container from the file content.
    /// </summary>
    /// <param name="content">The content container to remove.</param>
    public void RemoveContent(IContentContainer content)
    {
        if (content is null) throw new ArgumentNullException(nameof(content), "Content cannot be null.");
        ContentsSerialization.Remove(content);
    }

    /// <summary>
    /// Clears all content containers from the file content.
    /// </summary>
    public void ClearContents() => ContentsSerialization.Clear();
}
