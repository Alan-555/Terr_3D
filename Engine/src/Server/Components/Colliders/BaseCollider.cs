using OpenTK.Mathematics;
using Terr3D.Server.Shared;

namespace Terr3D.Server.Components;


public abstract class Collider(bool isStatic = false) : Component, ISupportsWireframe
{

    /// <summary>
    /// Whether this collider is static and managed by the Quadtree.
    /// </summary>
    public bool IsStatic { get; set; } = isStatic;

    /// <summary>
    /// Checks if this collider intersects with another BoxCollider.
    /// </summary>
    public abstract bool Intersects(BoxCollider other);

    /// <summary>
    /// Checks if this collider intersects with an axis-aligned bounding box.
    /// </summary>
    public abstract bool Intersects(Volume_AABB aabb);


    /// <summary>
    /// Checks if this collider intersects with an axis-aligned bounding box, ignoring the y component.
    /// </summary>
    public abstract bool IntersectsInfHeight(Volume_AABB aabb);

    /// <summary>
    /// Returns the non-rotated AABB that encompasses this rotated box.
    /// </summary>
    public abstract Volume_AABB GetAABB();

    /// <summary>
    /// Checks collision against an AABB and returns normal and depth for physics resolution.
    /// </summary>
    public abstract CollisionData Collide(Volume_AABB aabb);

    /// <summary>
    /// Checks if a ray intersects this collider.
    /// </summary>
    public abstract bool Raycast(Ray ray, out RaycastHit hit);

    public abstract Vector3 GetPos();

    public abstract Quaternion GetRot();

    public abstract Vector3 GetScale();
}