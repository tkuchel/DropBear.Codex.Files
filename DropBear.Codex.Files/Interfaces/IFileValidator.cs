using DropBear.Codex.Files.Models;
using DropBear.Codex.Validation.ReturnTypes;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileValidator
{
    Task<ValidationResult> ValidateFileAsync(DropBearFile file);
}
