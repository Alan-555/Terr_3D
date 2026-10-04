using Terr3D.Client.Resources;
using Terr3D.Utils;


/// <summary>
/// Use this class when you need to store data outside of the .NET runtime (e.g. uploading buffers to GPU). By using this class and the ResourceManager, you avoid leaking memory.
/// </summary>
/// <param name="path">The path to the resource</param>
public abstract class Resource : IDisposable
{

    public string Path {get; private set;} = "";
    public bool IsReleased {get; private set;}

    internal void Init(string path) => Path = path;

    protected abstract void Release();

    public void Dispose()
    {
        if(IsReleased) return;
        IsReleased = true;
        Release();
        GC.SuppressFinalize(this);
    }

    ~Resource()
    {
        if(!IsReleased)
            Diagnostics.Error($"Resource leak! {this} was never released.");
    }
}


internal sealed class ResourceEntry
{
    public string Path;
    public Type Type;
    public ResourceState State;
    public int RefCount;
    public Resource? Instance;
}


internal enum ResourceState
{
    Unloaded,
    LoadInProgress,
    InMemory,
    Errored
}