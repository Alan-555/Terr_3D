namespace Terr3D.Client.Resources;

internal sealed class ResourceScope(ResourceManager manager)
{
    private readonly ResourceManager manager = manager;
    private readonly List<ResourceEntry> held = new();
    private bool released;

    public T Use<T>(ResourceRef<T> r) where T : Resource
    {
        if (released) throw new ObjectDisposedException(nameof(ResourceScope));
        var entry = r.Entry ?? throw new ArgumentException("Invalid ResourceRef", nameof(r));

        if (!held.Contains(entry))
        {
            manager.Acquire(entry);
            held.Add(entry);
        }
        return (T)manager.GetInstance(entry);
    }

    public void ReleaseAll()
    {
        if (released) return;
        released = true;
        foreach (var e in held) manager.Release(e);
        held.Clear();
    }

}