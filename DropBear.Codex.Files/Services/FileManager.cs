#region

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using DropBear.Codex.AppLogger.Builders;
using DropBear.Codex.Core;
using DropBear.Codex.Files.Enums;
using DropBear.Codex.Files.Extensions;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.StorageManagers;
using Microsoft.Extensions.Logging;
using ZLogger;

#endregion

namespace DropBear.Codex.Files.Services;

[SupportedOSPlatform("windows")]
public class FileManager
{
    private readonly BlobStorageManager? _blobStorageManager;
    private readonly bool _isWindows = OperatingSystem.IsWindows();
    private readonly LocalStorageManager? _localStorageManager;
    private readonly ILogger<FileManager> _logger;
    private readonly StorageStrategy _storageStrategy;

    internal FileManager(StorageStrategy storageStrategy,
        LocalStorageManager? localStorageManager = null, BlobStorageManager? blobStorageManager = null)
    {
        if (!_isWindows)
        {
            throw new PlatformNotSupportedException("FileManager is only supported on Windows.");
        }

        _blobStorageManager = blobStorageManager;
        _localStorageManager = localStorageManager;
        _storageStrategy = storageStrategy;

        var loggerFactory = new LoggerConfigurationBuilder()
            .SetLogLevel(LogLevel.Information)
            .EnableConsoleOutput()
            .Build();

        _logger = loggerFactory.CreateLogger<FileManager>();
    }

    #region Public Methods

    public async Task<Result> WriteToFileAsync<T>(T data, string fullPath)
    {
        try
        {
            var validationResult = ValidateFilePath(fullPath);

            if (!validationResult.IsSuccess)
            {
                return Result.Failure(validationResult.ErrorMessage);
            }

            Stream? stream = null;

            if (typeof(T) == typeof(DropBearFile))
            {
                if (data is DropBearFile file)
                {
                    stream = await file.ToStreamAsync().ConfigureAwait(false);
                }
            }
            else if (typeof(T) == typeof(byte[]))
            {
                if (data is byte[] byteArray)
                {
                    stream = new MemoryStream(byteArray);
                }
            }
            else
            {
                return Result.Failure("Unsupported type for write operation.");
            }

            var writeResult = _storageStrategy switch
            {
                StorageStrategy.BlobOnly => await WriteToBlobStorage(fullPath, stream).ConfigureAwait(false),
                StorageStrategy.LocalOnly => await WriteToLocalStorage(fullPath, stream).ConfigureAwait(false),
                StorageStrategy.NoOperation => null,
                _ => null
            };

            if (writeResult is null || !writeResult.IsSuccess)
            {
                return Result.Failure(writeResult?.ErrorMessage ?? "Failed to write file.");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<T>> ReadFromFileAsync<T>(string fullPath)
    {
        try
        {
            var validationResult = ValidateFilePath(fullPath);
            if (!validationResult.IsSuccess)
            {
                return Result<T>.Failure(validationResult.ErrorMessage);
            }

            var streamResult = _storageStrategy switch
            {
                StorageStrategy.BlobOnly => await ReadFromBlobStorage(fullPath).ConfigureAwait(false),
                StorageStrategy.LocalOnly => await ReadFromLocalStorage(fullPath).ConfigureAwait(false),
                StorageStrategy.NoOperation => null,
                _ => null
            };

            if (streamResult is null || !streamResult.IsSuccess)
            {
                return Result<T>.Failure(streamResult?.ErrorMessage ?? "Failed to read file.");
            }

            var stream = streamResult.Value;

            if (typeof(T) != typeof(byte[]) || typeof(T) != typeof(DropBearFile))
            {
                return Result<T>.Failure("Unsupported type for read operation.");
            }

            if (typeof(T) == typeof(byte[]))
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms).ConfigureAwait(false);
                return Result<T>.Success((T)(object)ms.ToArray()); // Cast byte[] to T via object
            }

            if (typeof(T) == typeof(DropBearFile))
            {
                var file = await DropBearFileExtensions.FromStreamAsync(stream).ConfigureAwait(false);
                return Result<T>.Success((T)(object)file); // Cast DropBearFile to T via object
            }

            return Result<T>.Failure("Unsupported type for read operation.");
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result> UpdateFileAsync<T>(T data, string fullPath)
    {
        try
        {
            var validationResult = ValidateFilePath(fullPath);
            if (!validationResult.IsSuccess)
            {
                return Result.Failure(validationResult.ErrorMessage);
            }

            Stream? stream = default;

            if (typeof(T) == typeof(DropBearFile))
            {
                if (data is DropBearFile file)
                {
                    stream = await file.ToStreamAsync().ConfigureAwait(false);
                }
            }
            else if (typeof(T) == typeof(byte[]))
            {
                if (data is byte[] byteArray)
                {
                    stream = new MemoryStream(byteArray);
                }
            }
            else
            {
                return Result.Failure("Unsupported type for update operation.");
            }

            if (stream is null)
            {
                return Result.Failure("Failed to update file. Stream is null.");
            }

            var updateResult = _storageStrategy switch
            {
                StorageStrategy.BlobOnly => await UpdateBlobStorage(fullPath, stream).ConfigureAwait(false),
                StorageStrategy.LocalOnly => await UpdateLocalStorage(fullPath, stream).ConfigureAwait(false),
                StorageStrategy.NoOperation => null,
                _ => null
            };

            if (updateResult is null || !updateResult.IsSuccess)
            {
                return Result.Failure(updateResult?.ErrorMessage ?? "Failed to update file.");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, ex);
        }
    }

    public async Task<Result> DeleteFileAsync(string fullPath)
    {
        try
        {
            var validationResult = ValidateFilePath(fullPath);
            if (!validationResult.IsSuccess)
            {
                return Result.Failure(validationResult.ErrorMessage);
            }

            var deleteResult = _storageStrategy switch
            {
                StorageStrategy.BlobOnly => await DeleteFromBlobStorage(fullPath).ConfigureAwait(false),
                StorageStrategy.LocalOnly => await DeleteFromLocalStorage(fullPath).ConfigureAwait(false),
                StorageStrategy.NoOperation => null,
                _ => null
            };

            if (deleteResult is null || !deleteResult.IsSuccess)
            {
                return Result.Failure(deleteResult?.ErrorMessage ?? "Failed to delete file.");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, ex);
        }
    }

    #endregion

    #region Private Methods

    private Result ValidateFilePath(string fullPath)
    {
        var directoryPath = Path.GetDirectoryName(fullPath);
        if (directoryPath is null)
        {
            return Result.Failure("Invalid file path.");
        }

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            _logger.ZLogInformation($"Directory created successfully.");
        }

        if (HasWritePermissionOnDir(directoryPath))
        {
            return Result.Success();
        }

        _logger.ZLogInformation($"Setting write permissions on directory.");
        var currentUser = WindowsIdentity.GetCurrent().Name;

        AddDirectorySecurity(directoryPath, currentUser, FileSystemRights.WriteData, AccessControlType.Allow);

        return !HasWritePermissionOnDir(directoryPath)
            ? Result.Failure("Failed to set write permissions on directory.")
            : Result.Success();
    }

    private static bool HasWritePermissionOnDir(string path)
    {
        var dInfo = new DirectoryInfo(path);
        var dSecurity = dInfo.GetAccessControl();
        var rules = dSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
        foreach (FileSystemAccessRule rule in rules)
        {
            if ((rule.FileSystemRights & FileSystemRights.WriteData) is not FileSystemRights.WriteData)
            {
                if (rule.AccessControlType is AccessControlType.Allow)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void AddDirectorySecurity(string path, string account, FileSystemRights rights,
        AccessControlType controlType)
    {
        var dInfo = new DirectoryInfo(path);
        var dSecurity = dInfo.GetAccessControl();
        dSecurity.AddAccessRule(new FileSystemAccessRule(account, rights,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, controlType));
        dInfo.SetAccessControl(dSecurity);
    }

    #endregion

    #region Storage Helper Methods

    // Helper methods for each storage operation, each encapsulating the necessary logic for that storage type
    private async Task<Result> WriteToBlobStorage(string fullPath, Stream? stream)
    {
        try
        {
            var containerName = Path.GetDirectoryName(fullPath)?.Replace(@"\", "/", StringComparison.OrdinalIgnoreCase);
            var blobName = Path.GetFileName(fullPath);

            if (_blobStorageManager is null)
            {
                return Result.Failure("Blob storage manager is not set.");
            }

            if (containerName is not null)
            {
                await _blobStorageManager.WriteAsync(blobName, stream, containerName).ConfigureAwait(false);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Failed to write to blob storage.");
            return Result.Failure(ex.Message, ex);
        }
    }

    private async Task<Result> WriteToLocalStorage(string fullPath, Stream? stream)
    {
        try
        {
            if (_localStorageManager is not null)
            {
                await _localStorageManager
                    .WriteAsync(Path.GetFileName(fullPath), stream, Path.GetDirectoryName(fullPath))
                    .ConfigureAwait(false);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Failed to write to local storage.");
            return Result.Failure(ex.Message, ex);
        }
    }

    private async Task<Result<Stream>> ReadFromBlobStorage(string fullPath)
    {
        try
        {
            var containerName = Path.GetDirectoryName(fullPath)?.Replace(@"\", "/", StringComparison.OrdinalIgnoreCase);
            var blobName = Path.GetFileName(fullPath);
            if (_blobStorageManager is null)
            {
                throw new InvalidOperationException("Blob storage manager is not set.");
            }

            if (containerName is not null)
            {
                return await _blobStorageManager.ReadAsync(blobName, containerName).ConfigureAwait(false);
            }

            _logger.ZLogError($"Failed to read from blob storage. Container name is null.");
            return Result<Stream>.Failure("Failed to read from blob storage. Container name is null.");
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Failed to read from blob storage.");
            return Result<Stream>.Failure(ex.Message, ex);
        }
    }

    private async Task<Result<Stream>> ReadFromLocalStorage(string fullPath)
    {
        try
        {
            if (_localStorageManager is not null)
            {
                return await _localStorageManager.ReadAsync(Path.GetFileName(fullPath), Path.GetDirectoryName(fullPath))
                    .ConfigureAwait(false);
            }

            _logger.ZLogError($"Local storage manager is not set.");
            return Result<Stream>.Failure("Local storage manager is not set.");
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Failed to read from local storage.");
            return Result<Stream>.Failure(ex.Message, ex);
        }
    }

    private async Task<Result> UpdateBlobStorage(string fullPath, Stream? stream)
    {
        try
        {
            var containerName = Path.GetDirectoryName(fullPath)?.Replace(@"\", "/", StringComparison.OrdinalIgnoreCase);
            var blobName = Path.GetFileName(fullPath);

            if (_blobStorageManager is null)
            {
                return Result.Failure("Blob storage manager is not set.");
            }

            if (containerName is not null)
            {
                await _blobStorageManager.UpdateBlobAsync(blobName, stream, containerName).ConfigureAwait(false);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Failed to update blob storage.");
            return Result.Failure(ex.Message, ex);
        }
    }

    private async Task<Result> UpdateLocalStorage(string fullPath, Stream? stream)
    {
        try
        {
            if (_localStorageManager is not null)
            {
                await _localStorageManager
                    .UpdateAsync(Path.GetFileName(fullPath), stream, Path.GetDirectoryName(fullPath))
                    .ConfigureAwait(false);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Failed to update local storage.");
            return Result.Failure(ex.Message, ex);
        }
    }

    private async Task<Result> DeleteFromBlobStorage(string fullPath)
    {
        try
        {
            var containerName = Path.GetDirectoryName(fullPath)?.Replace(@"\", "/", StringComparison.OrdinalIgnoreCase);
            var blobName = Path.GetFileName(fullPath);

            if (_blobStorageManager is null)
            {
                return Result.Failure("Blob storage manager is not set.");
            }

            if (containerName is not null)
            {
                await _blobStorageManager.DeleteAsync(blobName, containerName).ConfigureAwait(false);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Failed to delete from blob storage.");
            return Result.Failure(ex.Message, ex);
        }
    }

    private async Task<Result> DeleteFromLocalStorage(string fullPath)
    {
        try
        {
            if (_localStorageManager is not null)
            {
                await _localStorageManager.DeleteAsync(Path.GetFileName(fullPath), Path.GetDirectoryName(fullPath))
                    .ConfigureAwait(false);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Failed to delete from local storage.");
            return Result.Failure(ex.Message, ex);
        }
    }

    #endregion
}
