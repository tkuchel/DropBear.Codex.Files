using DropBear.Codex.Files.Factory.Implementations;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Factory;

public static class FileManagerFactory
{
    public static IFileCreator FileCreator() => new FileCreator();
    public static IFileReader FileReader() => new FileReader();
    public static IFileDeleter FileDeleter() => new FileDeleter();
    public static IFileUpdater FileUpdater() => new FileUpdater();
    public static IFileValidator FileValidator() => new FileValidator();
    public static IFileIntegrityChecker FileIntegrityChecker() => new FileIntegrityChecker();
}
