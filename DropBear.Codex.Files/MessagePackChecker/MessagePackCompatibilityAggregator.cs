using System;
using System.Collections.Generic;
using DropBear.Codex.Core.ReturnTypes;

namespace DropBear.Codex.Files.MessagePackChecker
{
    /// <summary>
    /// Aggregator for checking MessagePack compatibility of types.
    /// </summary>
    public static class MessagePackCompatibilityAggregator
    {
        private static readonly MessagePackCompatibilityChecker MessagePackCompatibilityChecker = new();

        /// <summary>
        /// Checks the compatibility of a collection of types with MessagePack serialization.
        /// </summary>
        /// <param name="types">The collection of types to check.</param>
        /// <returns>The results of the compatibility check.</returns>
        public static MessagePackCompatibilityResults CheckTypes(IEnumerable<Type> types)
        {
            var results = new MessagePackCompatibilityResults();

            foreach (var type in types)
            {
                try
                {
                    // Use reflection to call the generic IsSerializable method
                    var method = typeof(MessagePackCompatibilityChecker)
                        .GetMethod(nameof(MessagePackCompatibilityChecker.IsSerializable));
                    var genericMethod = method?.MakeGenericMethod(type);
                    dynamic result = genericMethod?.Invoke(MessagePackCompatibilityChecker, null) ??
                                     Result.Failure("Failed to invoke compatibility check.");

                    if (result.IsSuccess)
                    {
                        results.SuccessTypes.Add(type.Name);
                    }
                    else
                    {
                        // Assuming IsSuccess is a property of the result, and result contains more detailed failure information
                        // Adjust this part based on the actual structure of your result object
                        string reason = result.ErrorMessage ?? "Failed compatibility check without specific reason.";
                        results.FailedTypes.Add(type.Name, reason);
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that might occur during the compatibility check
                    results.FailedTypes.Add(type.Name, ex.Message);
                }
            }

            return results;
        }
    }
}
