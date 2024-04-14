namespace DropBear.Codex.Files.Interfaces;

public interface IEncryptor
{
    byte[] Encrypt(byte[] data);
    byte[] Decrypt(byte[] data);
}
