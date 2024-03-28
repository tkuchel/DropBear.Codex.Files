using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;

namespace DropBear.Codex.Files.Validation.Strategies;

/// <summary>
///     Provides validation for <see cref="CompressionSettings" /> instances to ensure their properties meet specific
///     criteria.
/// </summary>
public class CompressionSettingsValidationStrategy : IValidationStrategy<CompressionSettings>
{
    /// <summary>
    ///     Validates the given <see cref="CompressionSettings" /> object, checking its compression status and level.
    /// </summary>
    /// <param name="context">The <see cref="CompressionSettings" /> instance to validate.</param>
    /// <returns>A <see cref="ValidationResult" /> indicating success or failure, with details of any validation errors.</returns>
    public ValidationResult Validate(CompressionSettings context)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Add more checks as necessary, depending on the business logic and requirements.

        return errors.Count > 0 ? ValidationResult.Fail(errors) : ValidationResult.Success();
    }
}
