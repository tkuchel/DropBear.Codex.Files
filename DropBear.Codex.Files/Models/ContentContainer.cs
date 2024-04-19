using System.Security.Cryptography;
using DropBear.Codex.Core;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Utilities.FeatureFlags;
using DropBear.Codex.Utilities.Hashing;
using DropBear.Codex.Utilities.Helpers;

namespace DropBear.Codex.Files.Models;

public class ContentContainer
{
    #region Constructors

    public ContentContainer()
    {
        _flagManager.AddFlag("IsDataSet");
        _flagManager.AddFlag("IsTemporaryDataSet");
        _flagManager.AddFlag("IsHashComputed");
        _flagManager.AddFlag("IsCompressed");
        _flagManager.AddFlag("IsEncrypted");
        _flagManager.AddFlag("IsSerialized");
        _flagManager.AddFlag("ShouldSerialize");
        _flagManager.AddFlag("ShouldCompress");
        _flagManager.AddFlag("ShouldEncrypt");
    }

    #endregion

    #region Private Methods

    private Result ComputeHash()
    {
        try
        {
            if (Data is null) return Result.Failure("The container data is null.");
            var hashResult = _hashingService.EncodeToBase64Hash(Data.ToArray());
            hashResult.OnSuccess(hash => Hash = hash);
            hashResult.OnFailure((em, ex) => throw new CryptographicException(em));
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    private Result SetDataFromByteArray(byte[]? data)
    {
        try
        {
            // Check if data is null or empty
            if (data is null || data.Length is 0) return Result.Failure("Data is null or empty.");

            // Set the data
            Data = data;

            // Set the IsDataSet flag
            _flagManager.SetFlag("IsDataSet");

            // Return success
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    private Result SetDataFromString(string? data)
    {
        try
        {
            // Check if data is null or empty
            if (string.IsNullOrEmpty(data)) return Result.Failure("Data is null or empty.");

            // Set the data
            Data = data.GetBytes();

            // Set the IsDataSet flag
            _flagManager.SetFlag("IsDataSet");

            // Return success
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    private Result SetDataFromClassType<T>(T data)
    {
        try
        {
            // Check if the data is null
            if (data is null) return Result.Failure("Data is null.");

            // Temporarily store the data until a serialization strategy is applied
            _temporaryData = data;

            // Set the IsSerialized flag
            _flagManager.SetFlag("IsTemporaryDataSet");

            // Return success
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    #endregion

    #region Public Methods

    public Result SetData<T>(T data)
    {
        // Set the ContentType to the AssemblyQualifiedName of T
        ContentType = typeof(T).AssemblyQualifiedName ?? "Unsupported/Unknown DataType";

        switch (data)
        {
            case byte[] byteArray:
                return SetDataFromByteArray(byteArray);
            case string str:
                return SetDataFromString(str);
            default:
                // Check if the type of data is a class
                if (data is not null && data.GetType().IsClass)
                    return SetDataFromClassType(data); // Handle all class types generically
                return Result.Failure("Data type not supported.");
        }
    }

    public Result ApplyCompression(ICompressionStrategy compressionStrategy)
    {
        // Check that data has been set before continuing.
        if (Data is null) return Result.Failure("Data is null.");

        try
        {
            var compressedData = compressionStrategy.ProcessData(Data.ToArray());
            if (compressedData.Length is 0)
                return Result.Failure("Compression failed.");
            Data = compressedData;
            _flagManager.SetFlag("IsCompressed");
            _transformations.Push("Compression");
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, ex);
        }
    }

    public Result ApplyEncryption(IEncryptionStrategy encryptionStrategy)
    {
        if (Data is null) return Result.Failure("Data is null.");

        try
        {
            var encryptedData = encryptionStrategy.ProcessData(Data.ToArray());
            if (encryptedData.Length is 0)
                return Result.Failure("Encryption failed.");
            Data = encryptedData;
            _flagManager.SetFlag("IsEncrypted");
            _transformations.Push("Encryption");
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, ex);
        }
    }

    public Result ApplySerialization(ISerializationStrategy serializationStrategy)
    {
        if (_temporaryData is null) return Result.Failure("Data is null.");

        try
        {
            var serializedData = serializationStrategy.ProcessData(_temporaryData);
            if (serializedData.Length is 0)
                return Result.Failure("Serialization failed.");
            Data = serializedData;
            _flagManager.SetFlag("IsSerialized");
            _transformations.Push("Serialization");
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, ex);
        }
    }

    #endregion


    #region Fields and Properties

    internal readonly DynamicFlagManager _flagManager = new();
    private readonly XxHashingService _hashingService = new();
    private readonly Stack<string> _transformations = new();
    private object? _temporaryData;
    public string ContentType { get; set; } = string.Empty;
    public IReadOnlyCollection<byte>? Data { get; private set; }
    public string? Hash { get; internal set; } = string.Empty;

    #endregion
}
