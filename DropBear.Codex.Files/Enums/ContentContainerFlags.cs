using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DropBear.Codex.Files.Enums;

[Flags]
public enum ContentContainerFlags
{
    [Description("No operation should be performed")] [JsonPropertyName("noOperation")]
    NoOperation = 1 << 0,

    [Description("Serialization should be skipped")] [JsonPropertyName("noSerialization")]
    NoSerialization = 1 << 1,

    [Description("Compression should be skipped")] [JsonPropertyName("noCompression")]
    NoCompression = 1 << 2,

    [Description("Encryption should be skipped")] [JsonPropertyName("noEncryption")]
    NoEncryption = 1 << 3,

    [Description("Data has been set")] [JsonPropertyName("dataIsSet")]
    DataIsSet = 1 << 4,

    [Description("Temporary data has been set")] [JsonPropertyName("temporaryDataIsSet")]
    TemporaryDataIsSet = 1 << 5,

    [Description("Serialization should be performed")] [JsonPropertyName("shouldSerialize")]
    ShouldSerialize = 1 << 6,

    [Description("Compression should be performed")] [JsonPropertyName("shouldCompress")]
    ShouldCompress = 1 << 7,

    [Description("Encryption should be performed")] [JsonPropertyName("shouldEncrypt")]
    ShouldEncrypt = 1 << 8
}
