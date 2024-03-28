using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;

namespace DropBear.Codex.Files.Validation.Strategies;

/// <summary>
///     Provides validation for <see cref="FileMetaData" /> instances, ensuring their properties meet specific criteria.
/// </summary>
public class FileMetaDataValidationStrategy : IValidationStrategy<FileMetaData>
{
    /// <summary>
    ///     Validates the <see cref="FileMetaData" /> context for errors based on predefined rules.
    /// </summary>
    /// <param name="context">The FileMetaData instance to validate.</param>
    /// <returns>
    ///     A <see cref="ValidationResult" /> indicating whether the validation was successful, and containing any errors
    ///     found.
    /// </returns>
    public ValidationResult Validate(FileMetaData context)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Validate Author is not null or whitespace
        if (string.IsNullOrWhiteSpace(context.Author))
            errors.Add("Author", "Author cannot be null or whitespace.");

        // Validate that there are content types provided
        if (context.ExpectedContentTypes.Count == 0)
            errors.Add("ExpectedContentTypes", "ExpectedContentTypes cannot be null or empty.");

        // Validate the VerificationHashes
        foreach (var contentType in context.ExpectedContentTypes)
            if (!context.VerificationHashes.ContainsKey(contentType) ||
                string.IsNullOrWhiteSpace(context.VerificationHashes[contentType]))
                errors.Add($"VerificationHash-{contentType.TypeName}", "Missing or invalid verification hash.");

        // Here, more validations can be added as needed.
        return errors.Count > 0 ? ValidationResult.Fail(errors) : ValidationResult.Success();
    }
}
