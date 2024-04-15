using System.Security.Cryptography;
using DropBear.Codex.Files.Interfaces;

public class ContentContainer
{
    private readonly Stack<string> transformations = new();
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
        if (transformations.Contains(strategyType, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"A {strategyType} strategy has already been applied.");

        Data = strategy.ProcessData(Data);
        transformations.Push(strategyType);
        UpdateStrategyFlags(strategy);
        ComputeHash();
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
    }

    public void RevertAllStrategies(ISerializationStrategy serializationStrategy)
    {
        while (transformations.Count > 0)
        {
            var strategyType = transformations.Pop();
            if (strategyType == "SerializationStrategy") RevertSerialization(serializationStrategy);
            // Logic to revert other strategies
        }
    }

    public static ContentContainer CreateFrom<T>(T obj, ISerializationStrategy serializationStrategy)
    {
        var container = new ContentContainer
        {
            ContentType = typeof(T).AssemblyQualifiedName ?? "Unidentifiable Content Type"
        };
        // Directly serialize the object to a byte array using the provided strategy
        var serializedData = serializationStrategy.ProcessData(obj);
        container.SetData(serializedData);
        container.ComputeHash();
        container.IsSerialized = true;
        // Log the application of the serialization strategy
        container.transformations.Push(serializationStrategy.GetType().Name);
        return container;
    }


    public void RevertSerialization(ISerializationStrategy serializationStrategy)
    {
        if (!string.IsNullOrEmpty(ContentType))
            try
            {
                var type = Type.GetType(ContentType);
                if (type == null)
                    throw new InvalidOperationException("Type information is invalid or not available.");

                var method = serializationStrategy.GetType().GetMethod(nameof(ISerializationStrategy.RevertData))
                    .MakeGenericMethod(type);
                Data = (byte[])method.Invoke(serializationStrategy, new object[] { Data });
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
