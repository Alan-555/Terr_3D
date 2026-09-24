using OpenTK.Mathematics;
using Terr3D.Server.Shared;

namespace Terr3D.Server.Components;


/// <summary>
/// A rotated box collider (OBB) that supports collision detection using the Separating Axis Theorem
/// </summary>
public class BoxCollider(Vector3 halfExtents, Vector3 centerOffset, bool isStatic = false) : Collider(isStatic), ISupportsWireframe
{
    public Vector3 HalfExtents = halfExtents;
    public Vector3 CenterOffset = centerOffset;

    /// <summary>
    /// The global center of the box collider.
    /// </summary>
    public Vector3 Center => Transform.Position + Vector3.Transform(CenterOffset, Transform.Rotation);


    public override Bounds GetBounds()
    {
        // Get the global axes of the box
        Vector3 axisX = Transform.Right;
        Vector3 axisY = Transform.Up;
        Vector3 axisZ = -Transform.Forward;

        // Inflate the AABB by projecting local axes onto global axes
        Vector3 worldHalfExtents = new Vector3(
            MathF.Abs(axisX.X * HalfExtents.X) + MathF.Abs(axisY.X * HalfExtents.Y) + MathF.Abs(axisZ.X * HalfExtents.Z),
            MathF.Abs(axisX.Y * HalfExtents.X) + MathF.Abs(axisY.Y * HalfExtents.Y) + MathF.Abs(axisZ.Y * HalfExtents.Z),
            MathF.Abs(axisX.Z * HalfExtents.X) + MathF.Abs(axisY.Z * HalfExtents.Y) + MathF.Abs(axisZ.Z * HalfExtents.Z)
        );

        return new Bounds(Center, worldHalfExtents);
    }


    public override bool Intersects(BoxCollider other)
    {
        //First check AABB
        if (!GetBounds().Intersects(other.GetBounds()))
            return false;

        //Now the complex SAT check
        return CheckSAT(other);
    }

    public override bool Intersects(Bounds aabb)
    {
        //First check AABB
        if (!GetBounds().Intersects(aabb))
            return false;

        //Now the complex SAT check
        return CheckSAT(aabb);
    }

    public override bool IntersectsInfHeight(Bounds aabb)
    {
        //augment the aabb with infinite height
        aabb = new(aabb.Position, aabb.HalfExtents with {Y = float.PositiveInfinity});
        return Intersects(aabb);
    }

    public override CollisionData Collide(Bounds aabb)
    {
        if (!GetBounds().Intersects(aabb))
            return new CollisionData { Intersects = false };


        Vector3[] axesA = [Transform.Right, Transform.Up, -Transform.Forward];
        Vector3[] axesB = [Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ];

        return PerformSATDetailed(aabb.Position, axesA, axesB, HalfExtents, aabb.HalfExtents);
    }

    public override bool Raycast(Ray ray, out RaycastHit hit)
    {
        //transform the ray to the local space
        Quaternion invRot = Transform.Rotation.Inverted();
        Vector3 localOrigin = Vector3.Transform(ray.Origin - Center, invRot);
        Vector3 localDir = Vector3.Transform(ray.Direction, invRot);

        Ray localRay = new(localOrigin, localDir);
        Bounds localAABB = new(Vector3.Zero, HalfExtents);

        //if the local ray intersects
        if (localAABB.Intersects(localRay, out float distance))
        {
            Vector3 localHitPoint = localRay.Origin + localRay.Direction * distance;
            Vector3 localNormal;

            float dx = MathF.Abs(MathF.Abs(localHitPoint.X) - HalfExtents.X);
            float dy = MathF.Abs(MathF.Abs(localHitPoint.Y) - HalfExtents.Y);
            float dz = MathF.Abs(MathF.Abs(localHitPoint.Z) - HalfExtents.Z);

            if (dx < dy && dx < dz) localNormal = new Vector3(MathF.Sign(localHitPoint.X), 0, 0);
            else if (dy < dz) localNormal = new Vector3(0, MathF.Sign(localHitPoint.Y), 0);
            else localNormal = new Vector3(0, 0, MathF.Sign(localHitPoint.Z));

            hit = new RaycastHit
            {
                Hit = true,
                Position = ray.Origin + ray.Direction * distance,
                Normal = Vector3.Transform(localNormal, Transform.Rotation).Normalized(),
                Distance = distance,
                Collider = this
            };

            //assume uniform scale TODO: stop assuming >:( (especially since I know it may not have a uniform scale)
            hit.Distance = (hit.Position - ray.Origin).Length;

            return true;
        }

        hit = RaycastHit.NoHit;
        return false;
    }

    private CollisionData PerformSATDetailed(Vector3 otherCenter, Vector3[] axesA, Vector3[] axesB, Vector3 halfA, Vector3 halfB)
    {
        Vector3 distance = otherCenter - Center;
        float minOverlap = float.MaxValue;
        Vector3 bestAxis = Vector3.Zero;

        // Collect all potential separating axes
        Span<Vector3> axesToCheck = stackalloc Vector3[15];
        axesA.AsSpan().CopyTo(axesToCheck[0..3]);
        axesB.AsSpan().CopyTo(axesToCheck[3..6]);

        int axisCount = 6;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Vector3 axis = Vector3.Cross(axesA[i], axesB[j]);
                if (axis.LengthSquared > 1e-6f)
                {
                    axesToCheck[axisCount++] = axis.Normalized();
                }
            }
        }

        for (int i = 0; i < axisCount; i++)
        {
            Vector3 axis = axesToCheck[i];
            float rA = MathF.Abs(Vector3.Dot(axesA[0] * halfA.X, axis)) +
                       MathF.Abs(Vector3.Dot(axesA[1] * halfA.Y, axis)) +
                       MathF.Abs(Vector3.Dot(axesA[2] * halfA.Z, axis));

            float rB = MathF.Abs(Vector3.Dot(axesB[0] * halfB.X, axis)) +
                       MathF.Abs(Vector3.Dot(axesB[1] * halfB.Y, axis)) +
                       MathF.Abs(Vector3.Dot(axesB[2] * halfB.Z, axis));

            float d = MathF.Abs(Vector3.Dot(distance, axis));

            float overlap = (rA + rB) - d;
            if (overlap < 0)
                return new CollisionData { Intersects = false };

            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                bestAxis = axis;
            }
        }

        // Ensure normal points from Center to otherCenter (out of the obstacle)
        if (Vector3.Dot(bestAxis, distance) < 0)
            bestAxis = -bestAxis;

        return new CollisionData
        {
            Intersects = true,
            Normal = bestAxis,
            Depth = minOverlap
        };
    }

    private bool CheckSAT(BoxCollider other)
    {
        Vector3[] axesA = [Transform.Right, Transform.Up, -Transform.Forward];
        Vector3[] axesB = [other.Transform.Right, other.Transform.Up, -other.Transform.Forward];

        return PerformSAT(other.Center, axesA, axesB, HalfExtents, other.HalfExtents);
    }

    private bool CheckSAT(Bounds aabb)
    {
        Vector3[] axesA = [Transform.Right, Transform.Up, -Transform.Forward];
        Vector3[] axesB = [Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ];

        return PerformSAT(aabb.Position, axesA, axesB, HalfExtents, aabb.HalfExtents);
    }

    private bool PerformSAT(Vector3 otherCenter, Vector3[] axesA, Vector3[] axesB, Vector3 halfA, Vector3 halfB)
    {
        Vector3 distance = otherCenter - Center;

        for (int i = 0; i < 3; i++) if (IsSeparatedOnAxis(axesA[i], axesA, axesB, halfA, halfB, distance)) return false;
        for (int i = 0; i < 3; i++) if (IsSeparatedOnAxis(axesB[i], axesA, axesB, halfA, halfB, distance)) return false;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Vector3 axis = Vector3.Cross(axesA[i], axesB[j]);
                if (axis.LengthSquared < 1e-6f) continue;
                if (IsSeparatedOnAxis(axis.Normalized(), axesA, axesB, halfA, halfB, distance)) return false;
            }
        }

        return true;
    }

    private bool IsSeparatedOnAxis(Vector3 axis, Vector3[] axesA, Vector3[] axesB, Vector3 halfA, Vector3 halfB, Vector3 distance)
    {
        float rA = MathF.Abs(Vector3.Dot(axesA[0] * halfA.X, axis)) +
                   MathF.Abs(Vector3.Dot(axesA[1] * halfA.Y, axis)) +
                   MathF.Abs(Vector3.Dot(axesA[2] * halfA.Z, axis));

        float rB = MathF.Abs(Vector3.Dot(axesB[0] * halfB.X, axis)) +
                   MathF.Abs(Vector3.Dot(axesB[1] * halfB.Y, axis)) +
                   MathF.Abs(Vector3.Dot(axesB[2] * halfB.Z, axis));

        float d = MathF.Abs(Vector3.Dot(distance, axis));

        return d > rA + rB;
    }

    public override Vector3 GetPos()
    {
        return Center;
    }

    public override Quaternion GetRot()
    {
        return Transform.Rotation;
    }

    public override Vector3 GetScale()
    {
        return HalfExtents * 2;
    }
}