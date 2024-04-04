using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileIntegrityChecker : IFileIntegrityChecker
{
    public async Task<bool> VerifyFileIntegrityAsync(Stream fileStream, IEnumerable<byte[]> components, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
