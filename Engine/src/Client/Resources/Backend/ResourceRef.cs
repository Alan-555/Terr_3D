namespace Terr3D.Client.Resources;

public readonly struct ResourceRef<T> where T : Resource
{
    /// <summary>
    /// the entry this ref points to
    /// </summary>
    internal readonly ResourceEntry? Entry;
    internal ResourceRef(ResourceEntry entry) => Entry = entry;

    public bool IsValid => Entry != null;
    public string Path => Entry?.Path ?? "";
}
