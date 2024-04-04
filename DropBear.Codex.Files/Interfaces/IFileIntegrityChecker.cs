namespace DropBear.Codex.Files.Interfaces;

public interface IFileIntegrityChecker
{
    Task<bool> VerifyFileIntegrityAsync(Stream fileStream, IEnumerable<byte[]> components,
        CancellationToken cancellationToken = default);
}
