using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Utilities.Hashing;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;

namespace DropBear.Codex.Files.Validation.Strategies;

/// <summary>
///     Provides validation for <see cref="FileContent" /> instances, ensuring that their contents meet specific criteria.
/// </summary>
public class FileContentValidationStrategy : IValidationStrategy<FileContent>
{
    private readonly Blake3HashingService _hasher = new();

    /// <summary>
    ///     Validates the <see cref="FileContent" /> instance, focusing on the integrity and validity of its contents.
    /// </summary>
    /// <param name="context">The FileContent instance to validate.</param>
    /// <returns>
    ///     A <see cref="ValidationResult" /> indicating the success or failure of the validation, including any errors
    ///     found.
    /// </returns>
    public ValidationResult Validate(FileContent context)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Ensure there are contents to validate
        if (context.Contents.Count is 0)
        {
            errors.Add("Contents", "FileContent must contain at least one content item.");
        }
        else
        {
            // Validate each content container
            var index = 0;
            foreach (var content in context.Contents)
                ValidateContentContainer(content, index++, errors);
        }

        return errors.Count > 0 ? ValidationResult.Fail(errors) : ValidationResult.Success();
    }

    /// <summary>
    ///     Validates a single content container, checking for data integrity and content type validity.
    /// </summary>
    /// <param name="content">The content container to validate.</param>
    /// <param name="index">The index of the content container within the FileContent's contents collection.</param>
    /// <param name="errors">A collection to which any validation errors should be added.</param>
    private void ValidateContentContainer(IContentContainer content, int index, Dictionary<string, string> errors)
    {
        // Validate the data is not null or empty
        if (content.GetData().Length is 0)
            errors.Add($"Data-{index}", $"Content at index {index} has null or empty data.");

        // Validate the content type is valid
        if (string.IsNullOrWhiteSpace(content.ContentType.TypeName))
            errors.Add($"ContentType-{index}", $"Content at index {index} has an invalid content type.");

        // Additional validation: Match the verification hash with the data
        if (!IsHashMatching(content.GetData(), content.VerificationHash))
            errors.Add($"VerificationHashMismatch-{index}",
                $"The verification hash does not match the data at index {index}.");
    }

    /// <summary>
    ///     Checks if the computed hash matches the expected hash.
    /// </summary>
    /// <param name="data">The data to compute the hash from.</param>
    /// <param name="expectedHash">The expected hash to compare against.</param>
    /// <returns>True if the computed hash matches the expected hash; otherwise, false.</returns>
    private bool IsHashMatching(byte[] data, string expectedHash)
    {
        var hashResult = _hasher.EncodeToBase64Hash(data);
        if (!hashResult.IsSuccess)
            // Log the error or handle the failure to hash the data as needed
            return false;

        // Compare the computed hash with the expected hash
        return hashResult.Value == expectedHash;
    }
}
