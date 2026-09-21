using OpenTK.Mathematics;

namespace Terr3D.Server.Shared;

public struct Ray
{
    public Vector3 Origin;
    public Vector3 Direction;
    public Vector3 InvDirection;

    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction.Normalized();
        InvDirection = new Vector3(1.0f / Direction.X, 1.0f / Direction.Y, 1.0f / Direction.Z);
    }
}
