using DropBear.Codex.Files.Factory.Implementations;
using DropBear.Codex.Files.Interfaces;
using ServiceStack.Text;

namespace DropBear.Codex.Files.Factory;

public static class FileManagerFactory
{
    private static readonly RecyclableMemoryStreamManager? StreamManager = new RecyclableMemoryStreamManager();
    public static IFileCreator FileCreator() => new FileCreator(StreamManager);
    public static IFileReader FileReader() => new FileReader(StreamManager);
    public static IFileDeleter FileDeleter() => new FileDeleter(StreamManager);
    public static IFileUpdater FileUpdater() => new FileUpdater(StreamManager);
    public static IFileValidator FileValidator() => new FileValidator(StreamManager);
    public static IFileIntegrityChecker FileIntegrityChecker() => new FileIntegrityChecker(StreamManager);
}
