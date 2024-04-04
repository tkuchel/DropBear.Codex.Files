using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Validation.ReturnTypes;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileValidator : IFileValidator
{
    public async Task<ValidationResult> ValidateFileAsync(DropBearFile file) => throw new NotImplementedException();
}
