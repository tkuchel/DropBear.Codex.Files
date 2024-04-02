using System;
using System.Collections.Generic;
using System.Reflection;
using DropBear.Codex.Files.Exceptions;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents
{
    /// <summary>
    /// Represents information about the content type.
    /// </summary>
    [MessagePackObject]
    public class ContentTypeInfo
    {
        // Static cache to hold loaded types for performance
        private static readonly Dictionary<string, Type> TypeCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Default constructor for MessagePack.
        /// </summary>
        [Obsolete("For MessagePack", false)]
        public ContentTypeInfo()
        {
            AssemblyName = string.Empty;
            TypeName = string.Empty;
            NameSpace = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentTypeInfo"/> class with the specified assembly name, type name, and namespace.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly containing the type.</param>
        /// <param name="typeName">The name of the type.</param>
        /// <param name="nameSpace">The namespace of the type.</param>
        public ContentTypeInfo(string assemblyName, string typeName, string nameSpace)
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
            NameSpace = nameSpace;
        }

        /// <summary>
        /// Gets or sets the name of the assembly containing the type.
        /// </summary>
        [Key(0)]
        public string AssemblyName { get; private set; }

        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        [Key(1)]
        public string TypeName { get; private set; }

        /// <summary>
        /// Gets or sets the namespace of the type.
        /// </summary>
        [Key(2)]
        public string NameSpace { get; private set; }

        private string GetFullTypeName() => $"{NameSpace}.{TypeName}";

        /// <summary>
        /// Gets the type corresponding to the content.
        /// </summary>
        /// <returns>The type corresponding to the content.</returns>
        public Type GetContentType()
        {
            var fullTypeName = GetFullTypeName();
            if (TypeCache.TryGetValue(fullTypeName, out var type)) return type;

            var assembly = Assembly.Load(AssemblyName);
            type = assembly.GetType(fullTypeName);

            if (type != null) TypeCache[fullTypeName] = type;

            return type ?? throw new ContentTypeNotFoundException("Content type not found.");
        }

        public override bool Equals(object? obj) =>
            obj is ContentTypeInfo info &&
            AssemblyName == info.AssemblyName &&
            TypeName == info.TypeName &&
            NameSpace == info.NameSpace;

        public override int GetHashCode() => HashCode.Combine(AssemblyName, TypeName, NameSpace);
    }
}
