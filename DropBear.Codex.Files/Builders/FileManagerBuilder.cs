using System.Runtime.Versioning;
using DropBear.Codex.Files.Services;
using FluentStorage;
using FluentStorage.Blobs;
using Microsoft.IO;

namespace DropBear.Codex.Files.Builders;

[SupportedOSPlatform("windows")]
public class FileManagerBuilder
{
    private readonly RecyclableMemoryStreamManager _memoryStreamManager = new();
    private string _accountKey = string.Empty;
    private string _accountName = string.Empty;
    private bool _enableBlobStorage;

    public FileManagerBuilder ConfigureBlobStorage(string accountName, string accountKey)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be null or empty.", nameof(accountName));
        if (string.IsNullOrWhiteSpace(accountKey))
            throw new ArgumentException("Account key cannot be null or empty.", nameof(accountKey));

        _accountName = accountName;
        _accountKey = accountKey;
        _enableBlobStorage = true;
        return this;
    }

    public FileManager Build()
    {
        if (_enableBlobStorage && (string.IsNullOrEmpty(_accountName) || string.IsNullOrEmpty(_accountKey)))
            throw new InvalidOperationException("Account name and key must be configured for blob storage.");

        IBlobStorage? blobStorage = null!;
        if (_enableBlobStorage)
            blobStorage = StorageFactory.Blobs.AzureBlobStorageWithSharedKey(_accountName, _accountKey);

        return new FileManager(_memoryStreamManager, blobStorage);
    }
}
