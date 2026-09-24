using Terr3D.Server.Shared;

namespace Terr3D.Server.Components;

public interface ISpatialBounds
{
    public Bounds GetBounds();
}