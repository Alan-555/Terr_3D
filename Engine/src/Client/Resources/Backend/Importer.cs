namespace Terr3D.Client.Resources;

internal interface IImporter
{
    Type ResourceType { get; }
    Resource Import(Stream data, string path);
}

public abstract class Importer<T> : IImporter where T : Resource
{
    protected abstract T Import(Stream data, string path);

    Type IImporter.ResourceType => typeof(T);
    Resource IImporter.Import(Stream data, string path) => Import(data, path);
}
