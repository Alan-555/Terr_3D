using OpenTK.Mathematics;
using Terr3D.Server.Components;

namespace Terr3D.Server.Shared;

public struct RaycastHit
{
    public bool Hit;
    public Vector3 Position;
    public Vector3 Normal;
    public float Distance;
    public Collider? Collider;

    public static RaycastHit NoHit => new RaycastHit { Hit = false, Distance = float.MaxValue };

    public override string ToString()
    {
        return Hit ? $"Hit at ${Position}\ndistance: ${Distance}"
        
         : "No hit";
    }
}
