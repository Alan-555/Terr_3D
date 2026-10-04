using Terr3D.Utils;

namespace Terr3D.Client.Resources;

/// <summary>
/// An abstract class for any GPU resource to be managed by the ResourceManager
/// </summary>
public abstract class GPU_Resource : Resource
{
    protected int _resHandle;

    public int Handle => _resHandle;


    public static implicit operator int(GPU_Resource res)
    {
        return res?._resHandle ?? 0; //res could be null make it null- uhh, I mean... zero instead
    }

    /// <summary>
    /// Releases the memory that was occupied by this resource
    /// </summary>
    protected abstract void ReleaseGPU_Resource();


    protected sealed override void Release()
    {
        if (_resHandle == 0)
        {
            Diagnostics.Warn("Disposing of a GPU_Resource that has already been disposed of!");
            return;
        }
        ReleaseGPU_Resource();
        _resHandle = 0;
    }
}

/// <summary>
/// Just an object that lives on the GPU. The ResourceManager takes no ownership of it, so it needs to be disposed of manually
/// </summary>
public abstract class GPU_Object
{
    protected int _handle;
    internal int Handle => _handle;

    protected GPU_Object(int handle) => _handle = handle;
    protected abstract void CollectObject();


    internal void Release()
    {
        if (_handle == 0)
        {
            Diagnostics.Warn("Disposing of a GPU_Resource that has already been disposed of!");
            return;
        }
        CollectObject();
        _handle = 0;
    }

    public static implicit operator int(GPU_Object res)
    {
        return res?._handle ?? 0; //res could be null make it null- uhh, I mean... zero instead
    }
}