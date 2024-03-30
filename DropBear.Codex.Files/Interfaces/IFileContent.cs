using System;
using System.Collections.Generic;
using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Interfaces;

/// <summary>
/// Defines the contract for managing and accessing the contents of a file.
/// </summary>
[MessagePack.Union(0, typeof(FileContent))]
public interface IFileContent
{
    /// <summary>
    /// Gets a read-only list of content containers within the file.
    /// </summary>
    IReadOnlyList<IContentContainer> Contents { get; }

    /// <summary>
    /// Adds a content container to the collection.
    /// </summary>
    /// <param name="content">The content container to be added. Must not be null.</param>
    void AddContent(IContentContainer content);

    /// <summary>
    /// Removes a specified content container from the collection.
    /// </summary>
    /// <param name="content">The content container to be removed. Must not be null.</param>
    void RemoveContent(IContentContainer content);

    /// <summary>
    /// Clears all content containers from the collection.
    /// </summary>
    void ClearContents();

    /// <summary>
    /// Retrieves a specific type of content from the collection, if present.
    /// </summary>
    /// <typeparam name="T">The type of content to retrieve.</typeparam>
    /// <returns>An instance of the requested content type if found; otherwise, null.</returns>
    T? GetContent<T>() where T : class;

    /// <summary>
    /// Retrieves the raw byte data for a specific type of content, identified by its content type name.
    /// </summary>
    /// <param name="contentTypeName">The name of the content type for which to retrieve the raw data.</param>
    /// <returns>The raw byte data if found; otherwise, null.</returns>
    byte[]? GetRawContent(string contentTypeName);
    
    /// <summary>
    /// Retrieves all contents of a specific type from the collection.
    /// </summary>
    /// <typeparam name="T">The type of content to retrieve.</typeparam>
    /// <returns>A collection of instances of the requested content type.</returns>
    IEnumerable<T> GetAllContents<T>() where T : class;
}
