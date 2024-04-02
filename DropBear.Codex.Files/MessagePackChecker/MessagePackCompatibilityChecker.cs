using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DropBear.Codex.Core.ReturnTypes;
using MessagePack;

namespace DropBear.Codex.Files.MessagePackChecker
{
    /// <summary>
    /// Utility class for checking MessagePack compatibility of types.
    /// </summary>
    public class MessagePackCompatibilityChecker
    {
        private readonly ConcurrentDictionary<Type, bool> _compatibilityCache = new();

        /// <summary>
        /// Checks if the given type is compatible with MessagePack serialization.
        /// </summary>
        /// <typeparam name="T">The type to check for MessagePack compatibility.</typeparam>
        /// <returns>A result indicating whether the type is compatible with MessagePack serialization.</returns>
        public Result IsSerializable<T>() where T : class => IsSerializable(typeof(T))
            ? Result.Success()
            : Result.Failure("Type is not compatible with MessagePack serialization.");

        /// <summary>
        /// Checks if the given type is compatible with MessagePack serialization.
        /// </summary>
        /// <param name="type">The type to check for MessagePack compatibility.</param>
        /// <returns>True if the type is compatible with MessagePack serialization, otherwise false.</returns>
        private bool IsSerializable(Type type)
        {
            // Return cached result if available
            if (_compatibilityCache.TryGetValue(type, out var isCompatible)) return isCompatible;

            try
            {
                // Perform compatibility checks
                isCompatible = PerformCompatibilityChecks(type);

                // Cache the result
                _compatibilityCache[type] = isCompatible;
            }
            catch
            {
                // Consider logging the exception or handling it as needed
                // Cache the negative result
                _compatibilityCache[type] = false;
                isCompatible = false;
            }

            return isCompatible;
        }

        private static bool PerformCompatibilityChecks(Type type)
        {
            if (!type.IsPublic || type.IsNested) return false;

            var messagePackObjectAttribute = type.GetCustomAttribute<MessagePackObjectAttribute>();
            if (messagePackObjectAttribute is null) return false; // Type must have MessagePackObject attribute

            var keyAsPropertyName = messagePackObjectAttribute.KeyAsPropertyName;

            if (keyAsPropertyName) return true; // No need to check for Key or IgnoreMember attributes

            var members = GetAllSerializableMembers(type);

            if (members.Any(member => member.GetCustomAttribute<KeyAttribute>() is null &&
                                      member.GetCustomAttribute<IgnoreMemberAttribute>() is null)) return false;

            if (type.IsInterface || type.IsAbstract) return EnsureValidUnionAttributes(type);

            return true;
        }

        private static IEnumerable<MemberInfo> GetAllSerializableMembers(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Cast<MemberInfo>()
                .Concat(type.GetFields(BindingFlags.Public | BindingFlags.Instance));

        private static bool EnsureValidUnionAttributes(Type type)
        {
            var unionAttributes = type.GetCustomAttributes<UnionAttribute>().ToList();
            if (unionAttributes.Count is 0 ||
                unionAttributes.Select(attr => attr.Key).Distinct().Take(unionAttributes.Count + 1).Count() !=
                unionAttributes.Count) return false;

            foreach (var attr in unionAttributes)
            {
                if (!attr.SubType.IsSubclassOf(type) && !type.IsAssignableFrom(attr.SubType)) return false;
                if (attr.SubType.GetCustomAttribute<MessagePackObjectAttribute>() is null) return false;
            }

            return true;
        }
    }
}
