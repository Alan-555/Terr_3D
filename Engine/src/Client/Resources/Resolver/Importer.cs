namespace Terr3D.Client.Resources;

internal interface IImporter
{
    Type ResourceType { get; }
    Resource Load(ResourceEntry entry);
}

public abstract class Importer<T> : IImporter where T : Resource
{
    protected abstract T Import(string path);

    Type IImporter.ResourceType => typeof(T);
    Resource IImporter.Load(ResourceEntry entry) => Import(entry.Path);
}