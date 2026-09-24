using OpenTK.Mathematics;
using Terr3D.Server.Shared;

namespace Terr3D.Server.Components;


/// <summary>
/// An axis-aligned box collider (AABB) that supports collision detection
/// </summary>
public class AABB_Collider(Vector3 halfExtents, Vector3 centerOffset, bool isStatic = false) : Collider(isStatic), ISupportsWireframe
{
    private readonly Bounds _baseAABB = new(centerOffset, halfExtents);

    public Bounds AABB => _baseAABB with { Position = _baseAABB.Position + Transform.Position };

    public override CollisionData Collide(Bounds aabb)
    {
        throw new NotImplementedException();
    }

    public override Bounds GetBounds()
    {
        return AABB;
    }

    public override bool Intersects(BoxCollider other)
    {
        return other.Intersects(AABB);
    }

    public override bool Intersects(Bounds aabb)
    {
        return aabb.Intersects(AABB);
    }

    public override bool IntersectsInfHeight(Bounds aabb)
    {
        return aabb.IntersectsInfHeight(AABB);
    }

    public override bool Raycast(Ray ray, out RaycastHit hit)
    {
        if (AABB.Intersects(ray, out float distance))
        {
            Vector3 hitPoint = ray.Origin + ray.Direction * distance;
            Vector3 localHit = hitPoint - AABB.Position;
            Vector3 normal;

            float dx = MathF.Abs(MathF.Abs(localHit.X) - AABB.HalfExtents.X);
            float dy = MathF.Abs(MathF.Abs(localHit.Y) - AABB.HalfExtents.Y);
            float dz = MathF.Abs(MathF.Abs(localHit.Z) - AABB.HalfExtents.Z);

            if (dx < dy && dx < dz) normal = new Vector3(MathF.Sign(localHit.X), 0, 0);
            else if (dy < dz) normal = new Vector3(0, MathF.Sign(localHit.Y), 0);
            else normal = new Vector3(0, 0, MathF.Sign(localHit.Z));

            hit = new RaycastHit
            {
                Hit = true,
                Position = hitPoint,
                Normal = normal,
                Distance = distance,
                Collider = this
            };
            return true;
        }

        hit = RaycastHit.NoHit;
        return false;
    }


    public override Vector3 GetPos()
    {
        return AABB.Position;
    }

    public override Quaternion GetRot()
    {
        return Quaternion.Identity; //AABBs do not rotate
    }

    public override Vector3 GetScale()
    {
        return AABB.HalfExtents * 2;
    }
}