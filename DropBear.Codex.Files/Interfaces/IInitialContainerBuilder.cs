namespace DropBear.Codex.Files.Interfaces;

public interface IInitialContainerBuilder
{
    IDataConfigurable WithObject<T>(T obj);
    IBuildable WithData(byte[] data);
}
