#region

using DropBear.Codex.Files.Enums;

#endregion

namespace DropBear.Codex.Files.Interfaces;

public interface IStorageOptionsStep
{
    IStorageOptionsStep WithBlobStorage(string accountName, string accountKey, string containerName);
    IStorageOptionsStep WithLocalStorage(string basePath);
    IStorageOptionsStep WithStorageStrategy(StorageStrategy strategy);
    IBuildStep Configure();
}
