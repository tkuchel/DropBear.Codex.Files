using Microsoft.IO;

namespace DropBear.Codex.Files.Interfaces;

public interface IMemoryStreamManagerStep
{
    IStorageOptionsStep WithMemoryStreamManager(RecyclableMemoryStreamManager memoryStreamManager);
}
