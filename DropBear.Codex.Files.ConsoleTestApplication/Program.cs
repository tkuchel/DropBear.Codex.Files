using DropBear.Codex.Files.Compression;
using DropBear.Codex.Files.Encryption;
using DropBear.Codex.Files.Hashing;
using DropBear.Codex.Files.Providers;
using DropBear.Codex.Files.Serialization;
using Kokuban;
using MessagePack;
using MessagePackSerializer = DropBear.Codex.Files.Serialization.MessagePackSerializer;

namespace DropBear.Codex.Files.ConsoleTestApplication;

internal class Program
{
    private const string publicKeyPath = @"C:\Temp\publicKey.xml";
    private const string privateKeyPath = @"C:\Temp\privateKey.xml";

    private static async Task Main(string[] args)
    {
        Console.WriteLine(Chalk.Blue + "Starting test application");

        // Create a test file to work with
        var testFile = new TestFile("test.txt", DateTime.Now, "Hello, world!");

        // Serialization Tests
        Console.WriteLine(Chalk.Yellow + "Serialization Tests");
        var jsonSerializer = new JsonSerializer();
        var serializedTestFile = jsonSerializer.Serialize(testFile);
        Console.WriteLine(Chalk.Green + "JSON: " + Chalk.White + serializedTestFile);

        var msgPackSerializer = new MessagePackSerializer();
        var msgPackSerializedTestFile = msgPackSerializer.Serialize(testFile);
        Console.WriteLine(Chalk.Green + "MSGPACK: " + Chalk.White + msgPackSerializedTestFile);

        // Deserialization Tests
        Console.WriteLine(Chalk.Yellow + "Deserialization Tests");
        var deserializedTestFile = jsonSerializer.Deserialize<TestFile>(serializedTestFile);
        Console.WriteLine(Chalk.Green + "JSON: " + Chalk.White +  deserializedTestFile);

        var msgPackDeserializedTestFile = msgPackSerializer.Deserialize<TestFile>(msgPackSerializedTestFile);
        Console.WriteLine(Chalk.Green + "MSGPACK: " + Chalk.White +  msgPackDeserializedTestFile);

        // Setup RSA key pair for encryption
        var rsaKeyProvider = new RsaKeyProvider(publicKeyPath, privateKeyPath);
        var rsa = rsaKeyProvider.GetRsaProvider();

        // Encryption Tests
        Console.WriteLine(Chalk.Yellow + "Encryption Tests");
        var aesEncryptor = new AesEncryptor(rsa);
        var encryptedTestFile = aesEncryptor.Encrypt(jsonSerializer.SerializeToByteArray(testFile));
        Console.WriteLine(Chalk.Green + "AESCNG: ");
        WriteByteArrayToConsole(encryptedTestFile);

        var aesgcmEncryptor = new AesGcmEncryptor(rsa);
        var encryptedTestFileGcm = aesgcmEncryptor.Encrypt(jsonSerializer.SerializeToByteArray(testFile));
        Console.WriteLine(Chalk.Green + "AESGCM: ");
        WriteByteArrayToConsole(encryptedTestFileGcm);

        // Decryption Tests
        Console.WriteLine(Chalk.Yellow + "Decryption Tests");
        var decryptedTestFile = aesEncryptor.Decrypt(encryptedTestFile);
        Console.WriteLine(Chalk.Green + "AESCNG: ");
        WriteByteArrayToConsole(decryptedTestFile);
        
        var decryptedTestFileGcm = aesgcmEncryptor.Decrypt(encryptedTestFileGcm);
        Console.WriteLine(Chalk.Green + "AESGCM: ");
        WriteByteArrayToConsole(decryptedTestFileGcm);

        // Hashing Tests
        Console.WriteLine(Chalk.Yellow + "Hashing Tests");
        var sha256Hasher = new SHA256Hasher();
        var hashedTestFile = sha256Hasher.Hash(jsonSerializer.SerializeToByteArray(testFile));
        Console.WriteLine(Chalk.Green + "SHA256: " + hashedTestFile);

        var hmacSha256Hasher = new HmacSha256Hasher(rsa);
        var hmacHashedTestFile = hmacSha256Hasher.Hash(jsonSerializer.SerializeToByteArray(testFile));
        Console.WriteLine(Chalk.Green + "HMACSHA256: " + hmacHashedTestFile);

        // Compression Tests
        Console.WriteLine(Chalk.Yellow + "Compression Tests");
        var gzipCompressor = new GZipCompressor();
        var compressedTestFile = gzipCompressor.Compress(jsonSerializer.SerializeToByteArray(testFile));
        Console.WriteLine(Chalk.Green + "COMPRESSED: ");
        WriteByteArrayToConsole(compressedTestFile);

        // Decompression Tests
        Console.WriteLine(Chalk.Yellow + "Decompression Tests");
        var decompressedTestFile = gzipCompressor.Decompress(compressedTestFile);
        Console.WriteLine(Chalk.Green + "DECOMPRESSED: ");
        WriteByteArrayToConsole(decompressedTestFile);

        Console.WriteLine(Chalk.Blue + "End of test application");
    }
    
    public static void WriteByteArrayToConsole(byte[] byteArray)
    {
        // Convert each byte in the byte array to a hexadecimal string and concatenate them
        Console.WriteLine("Byte array:");
        foreach (byte b in byteArray)
        {
            Console.Write($"{b:X2} "); // X2 formats the byte in two-digit hexadecimal
        }
        Console.WriteLine(); // Add a newline for better readability
    }

}

[MessagePackObject(true)]
public class TestFile
{
    public TestFile()
    {
    }

    public TestFile(string name, DateTime createdAt, string content)
    {
        Name = name;
        CreatedAt = createdAt;
        Content = content;
    }

    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Content { get; set; }
}