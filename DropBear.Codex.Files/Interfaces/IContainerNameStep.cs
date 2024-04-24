namespace DropBear.Codex.Files.Interfaces;

public interface IContainerNameStep
{
    IBuildStep WithDefaultContainerName(string containerName);
}
