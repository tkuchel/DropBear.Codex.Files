#region

using Microsoft.IO;

#endregion

namespace DropBear.Codex.Files.Interfaces;

public interface IMemoryStreamManagerStep
{
    IStorageOptionsStep WithMemoryStreamManager(RecyclableMemoryStreamManager memoryStreamManager);
}
