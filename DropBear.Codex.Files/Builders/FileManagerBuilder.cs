#region

using System.Runtime.Versioning;
using DropBear.Codex.Files.Enums;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Services;
using DropBear.Codex.Files.StorageManagers;
using FluentStorage;
using FluentStorage.Blobs;
using Microsoft.IO;

#endregion

namespace DropBear.Codex.Files.Builders;

[SupportedOSPlatform("windows")]
public class FileManagerBuilder : IMemoryStreamManagerStep, IStorageOptionsStep, IBuildStep
{
    private readonly bool _isWindows = OperatingSystem.IsWindows();
    private BlobStorageManager? _blobStorageManager;
    private LocalStorageManager? _localStorageManager;
    private RecyclableMemoryStreamManager? _memoryStreamManager;
    private StorageStrategy _storageStrategy;

    public FileManager Build()
    {
        if (!_isWindows)
        {
            throw new PlatformNotSupportedException("FileManager is only supported on Windows.");
        }

        if (_memoryStreamManager is null)
        {
            throw new InvalidOperationException("MemoryStreamManager is required.");
        }

        // Ensure at least one storage manager is configured
        if (_blobStorageManager is null && _localStorageManager is null)
        {
            throw new InvalidOperationException("At least one storage manager must be configured.");
        }

        return new FileManager(_storageStrategy, _localStorageManager, _blobStorageManager);
    }

    public IStorageOptionsStep WithMemoryStreamManager(RecyclableMemoryStreamManager memoryStreamManager)
    {
        _memoryStreamManager = memoryStreamManager ?? throw new ArgumentNullException(nameof(memoryStreamManager));
        return this;
    }

    public IStorageOptionsStep WithStorageStrategy(StorageStrategy strategy)
    {
        _storageStrategy = strategy;
        return this;
    }

    public IBuildStep Configure()
    {
        return this;
        // All required configurations are completed, move to build step.
    }

    public IStorageOptionsStep WithBlobStorage(string accountName, string accountKey, string containerName)
    {
        IBlobStorage blobStorage = StorageFactory.Blobs.AzureBlobStorageWithSharedKey(accountName, accountKey);
        if (_memoryStreamManager is not null)
        {
            _blobStorageManager = new BlobStorageManager(blobStorage, _memoryStreamManager, containerName);
        }

        return this;
    }

    public IStorageOptionsStep WithLocalStorage(string basePath)
    {
        if (_memoryStreamManager is not null)
        {
            _localStorageManager = new LocalStorageManager(_memoryStreamManager, basePath);
        }

        return this;
    }

    public static IMemoryStreamManagerStep Create()
    {
        return new FileManagerBuilder();
    }
}
