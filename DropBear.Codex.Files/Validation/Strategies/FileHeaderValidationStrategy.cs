using DropBear.Codex.Files.Models.FileComponents.MainComponents;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;

namespace DropBear.Codex.Files.Validation.Strategies;

/// <summary>
///     Provides validation for <see cref="FileHeader" /> instances, ensuring their properties meet specific criteria.
/// </summary>
public class FileHeaderValidationStrategy : IValidationStrategy<FileHeader>
{
    /// <summary>
    ///     Validates the given <see cref="FileHeader" /> object, checking its version, file signature, and other properties.
    /// </summary>
    /// <param name="context">The FileHeader instance to validate.</param>
    /// <returns>A ValidationResult indicating success or failure, with details of any validation errors.</returns>
    public ValidationResult Validate(FileHeader context)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Validate FileSignature
        ValidateFileSignature(context.Signature, errors);

        return errors.Count is not 0 ? ValidationResult.Fail(errors) : ValidationResult.Success();
    }

    /// <summary>
    ///     Validates properties of the <see cref="FileSignature" />.
    /// </summary>
    /// <param name="signature">The FileSignature to validate.</param>
    /// <param name="errors">A collection of errors to be populated if validation fails.</param>
    private static void ValidateFileSignature(FileSignature signature, Dictionary<string, string> errors)
    {
        // Check signature length
        if (signature.Signature.Length is 0) errors.Add("FileSignature.Signature", "Signature cannot be empty.");

        // MediaType validation
        if (string.IsNullOrWhiteSpace(signature.MediaType))
            errors.Add("FileSignature.MediaType", "MediaType cannot be null or empty.");

        // Extension validation
        if (string.IsNullOrWhiteSpace(signature.Extension))
            errors.Add("FileSignature.Extension", "Extension cannot be null or empty.");

        // HeaderLength and Offset validation could be added here based on specific rules
    }
}
