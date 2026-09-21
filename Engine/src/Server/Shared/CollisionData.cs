using OpenTK.Mathematics;

namespace Terr3D.Server.Shared;

/// <summary>
/// Contains detailed information about a collision.
/// </summary>
public struct CollisionData
{
    /// <summary>
    /// The collision normal, pointing from the obstacle towards the dynamic object.
    /// </summary>
    public Vector3 Normal;

    /// <summary>
    /// How much the objects are overlapping.
    /// </summary>
    public float Depth;

    /// <summary>
    /// Whether a collision actually occurred.
    /// </summary>
    public bool Intersects;
}