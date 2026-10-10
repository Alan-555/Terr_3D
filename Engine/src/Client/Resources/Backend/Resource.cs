using Terr3D.Client.Resources;
using Terr3D.Utils;

namespace Terr3D.Client.Resources;
/// <summary>
/// Use this class when you need to store data outside of the .NET runtime (e.g. uploading buffers to GPU). By using this class and the ResourceManager, you avoid leaking memory.
/// </summary>
/// <param name="path">The path to the resource</param>
public abstract class Resource
{

    public const string DynamicRes = $"dynamic://";
    public const string BuiltinRes = $"builtin://";

    public string Path { get; private set; } = "";
    public bool IsReleased { get; private set; }

    internal void Init(string path) => Path = path;

    protected abstract void Release();

    internal void Collect()
    {
        if (IsReleased) return;
        IsReleased = true;
        Release();
        GC.SuppressFinalize(this);
    }

    ~Resource()
    {
        if (!IsReleased)
            Diagnostics.Error($"Resource leak! {this} was never released.");
    }

    public override string ToString() => $"{GetType().Name}({Path})";
}


internal sealed class ResourceEntry
{
    public string Path;
    public Type Type;
    public ResourceState State;
    public int RefCount;
    public Resource? Instance;
    public Func<Resource>? Factory;
    public bool Pinned;
}


internal enum ResourceState
{
    Unloaded,
    LoadInProgress,
    InMemory,
    Errored
}