using Terr3D.Utils;

namespace Terr3D.Client.Resources;

/// <summary>
/// An abstract class for any GPU resource to be managed. Includes the resource's int handle and a virtual method to dispose of this resource.
/// </summary>
public abstract class GPU_Resource : IDisposable
{
    public int _resHandle;


    public static implicit operator int(GPU_Resource res)
    {
        return res?._resHandle ?? 0; //res could be null make it null- uhh, I mean... zero instead
    }
    
    ///<summary>
    /// Call with base to eliminate dangling pointer!
    /// <inheritdoc/>
    /// </summary>
    public virtual void Dispose()
    {
        if(_resHandle == 0)
        {
            Diagnostics.Warn("Disposing of a GPU_Resource that has already been disposed of!");
        }
        _resHandle = 0;
    }
}