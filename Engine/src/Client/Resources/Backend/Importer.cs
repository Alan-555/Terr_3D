namespace Terr3D.Client.Resources;

internal interface IImporter
{
    Type ResourceType { get; }
    Resource Import(Stream data, ResourceEntry entry);
}

public abstract class Importer<T> : IImporter where T : Resource
{
    protected abstract T Import(Stream data, string path);

    Type IImporter.ResourceType => typeof(T);
    Resource IImporter.Import(Stream data, ResourceEntry entry) => Import(data, entry.Path);
}
