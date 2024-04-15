using System.Security.Cryptography;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Models;

public class ContentContainer
{
    private readonly Stack<string> _transformations = new();
    private readonly Dictionary<string, IContentStrategy> _strategyInstances = new(StringComparer.OrdinalIgnoreCase);
    private ISerializationStrategy? _serializationStrategy;
 
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; private set; }
    public string Hash { get; internal set; }
    public bool IsCompressed { get; private set; }
    public bool IsEncrypted { get; private set; }
    public bool IsHashed { get; private set; }
    public bool IsSerialized { get; set; }
    public void ApplyStrategy(IContentStrategy strategy)
    {
        var strategyType = strategy.GetType().Name;
        if (_transformations.Contains(strategyType, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"A {strategyType} strategy has already been applied.");

        Data = strategy.ProcessData(Data);
        _transformations.Push(strategyType);
        UpdateStrategyFlags(strategy);
        ComputeHash();
        _strategyInstances[strategyType] = strategy;
    }
    private void UpdateStrategyFlags(IContentStrategy strategy)
    {
        switch (strategy)
        {
            case ICompressionStrategy:
                IsCompressed = true;
                break;
            case IEncryptionStrategy:
                IsEncrypted = true;
                break;
            case IHashingStrategy:
                IsHashed = true;
                break;
        }
    }
    public void SetData(byte[] data)
    {
        if (data is null || data.Length is 0)
            throw new ArgumentException("Data cannot be null or empty.", nameof(data));
        Data = data;
        ComputeHash();
        IsSerialized = false;
    }
    public void RevertAllStrategies()
    {
        while (_transformations.Count > 0)
        {
            var strategyType = _transformations.Pop();
            if (strategyType is "SerializationStrategy")
            {
                RevertSerialization(_serializationStrategy);
            }
            else
            {
                if (_strategyInstances.TryGetValue(strategyType, out var strategy))
                    Data = strategy.RevertData(Data);
                else
                    throw new InvalidOperationException($"No strategy instance found for {strategyType}.");
            }
        }
    }
    public static ContentContainer CreateFrom<T>(T obj, ISerializationStrategy serializationStrategy)
    {
        var container = new ContentContainer
        {
            ContentType = typeof(T).AssemblyQualifiedName ?? string.Empty,
        };
        
        // Directly serialize the object to a byte array using the provided strategy
        var serializedData = serializationStrategy.ProcessData(obj);
        container.SetData(serializedData);
        container.ComputeHash();
        container.IsSerialized = true;
        
        // Log the application of the serialization strategy
        container._transformations.Push(serializationStrategy.GetType().Name);
        container._serializationStrategy = serializationStrategy;
        return container;
    }
    private void RevertSerialization(ISerializationStrategy? serializationStrategy)
    {
        if (!string.IsNullOrEmpty(ContentType))
            try
            {
                var type = Type.GetType(ContentType);
                if (type == null)
                    throw new InvalidOperationException("Type information is invalid or not available.");

                var method = serializationStrategy.GetType().GetMethod(nameof(ISerializationStrategy.RevertData))
                    ?.MakeGenericMethod(type);
                if (method != null)
                    Data = (byte[])method.Invoke(serializationStrategy, new object[] { Data })! ??
                           throw new InvalidOperationException("Failed to revert serialization.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to revert serialization.", ex);
            }
        else
            throw new InvalidOperationException("ContentType must be set before deserialization can occur.");
    }
    public void ComputeHash() => Hash = Convert.ToBase64String(SHA256.HashData(Data));
    public bool VerifyHash(string hash) => Hash == hash;
}