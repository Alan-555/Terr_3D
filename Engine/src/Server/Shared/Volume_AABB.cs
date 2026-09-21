using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using Terr3D.Server.Components;

namespace Terr3D.Server.Shared;

/// <summary>
/// Represents a rectangular volume
/// </summary>
public struct Volume_AABB
{
    public Vector3 Position;
    public Vector3 HalfExtents;

    public Vector3 MinPoint => Position - HalfExtents;
    public Vector3 MaxPoint => Position + HalfExtents;

    public Volume_AABB(Vector3 position, Vector3 halfExtents)
    {
        Position = position;
        HalfExtents = halfExtents;
    }

    public static Volume_AABB FromTwoPoints(Vector3 minPoint, Vector3 maxPoint)
    {
        var half = (maxPoint - minPoint) / 2f;
        var position = half + minPoint;
        return new(position, half);
    }

    public bool PointInVolumeInfHeight(Vector3 point)
    {
        return point.X >= MinPoint.X &&
               point.Z >= MinPoint.Z &&

               point.X <= MaxPoint.X &&
               point.Z <= MaxPoint.Z;

    }

    public bool PointInVolume(Vector3 point)
    {
        return point.X >= MinPoint.X &&
               point.Y >= MinPoint.Y &&
               point.Z >= MinPoint.Z &&

               point.X <= MaxPoint.X &&
               point.Y <= MaxPoint.Y &&
               point.Z <= MaxPoint.Z;
    }

    public bool Intersects(Volume_AABB other)
    {
        return Math.Abs(Position.X - other.Position.X) <= (HalfExtents.X + other.HalfExtents.X) &&
               Math.Abs(Position.Y - other.Position.Y) <= (HalfExtents.Y + other.HalfExtents.Y) &&
               Math.Abs(Position.Z - other.Position.Z) <= (HalfExtents.Z + other.HalfExtents.Z);
    }

    public bool Intersects(Ray ray, out float distance)
    {
        float t1 = (MinPoint.X - ray.Origin.X) * ray.InvDirection.X;
        float t2 = (MaxPoint.X - ray.Origin.X) * ray.InvDirection.X;
        float t3 = (MinPoint.Y - ray.Origin.Y) * ray.InvDirection.Y;
        float t4 = (MaxPoint.Y - ray.Origin.Y) * ray.InvDirection.Y;
        float t5 = (MinPoint.Z - ray.Origin.Z) * ray.InvDirection.Z;
        float t6 = (MaxPoint.Z - ray.Origin.Z) * ray.InvDirection.Z;

        float tmin = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
        float tmax = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

        if (tmax < 0 || tmin > tmax || float.IsNaN(tmax))
        {
            distance = 0;
            return false;
        }

        distance = tmin < 0 ? tmax : tmin;
        return true;
    }

    public bool IntersectsInfHeight(Volume_AABB other)
    {
        return Math.Abs(Position.X - other.Position.X) <= (HalfExtents.X + other.HalfExtents.X) &&
               Math.Abs(Position.Z - other.Position.Z) <= (HalfExtents.Z + other.HalfExtents.Z);
    }

    public override string ToString() => $"{Position}±{HalfExtents}";


    /// <summary>
    /// Determines whether a volume is a) fully in camera's frustum, b) partially or c) not at all
    /// </summary>
    /// <param name="camera">The camera</param>
    /// <param name="volume">The volume to test visibility for</param>
    /// <returns>The classification</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Visibility ClassifyVisibilityForFrustum(Matrix4 vp, Volume_AABB volume)
    {
        //Gribb-Hartmann method
        Vector4[] planes =
        [
            //Left
            new Vector4(vp.M14 + vp.M11, vp.M24 + vp.M21, vp.M34 + vp.M31, vp.M44 + vp.M41).Normalized(),
            //Right
            new Vector4(vp.M14 - vp.M11, vp.M24 - vp.M21, vp.M34 - vp.M31, vp.M44 - vp.M41).Normalized(),
            //Bottom
            new Vector4(vp.M14 + vp.M12, vp.M24 + vp.M22, vp.M34 + vp.M32, vp.M44 + vp.M42).Normalized(),
            //Top
            new Vector4(vp.M14 - vp.M12, vp.M24 - vp.M22, vp.M34 - vp.M32, vp.M44 - vp.M42).Normalized(),
            //Near
            new Vector4(vp.M14 + vp.M13, vp.M24 + vp.M23, vp.M34 + vp.M33, vp.M44 + vp.M43).Normalized(),
            //Far
            new Vector4(vp.M14 - vp.M13, vp.M24 - vp.M23, vp.M34 - vp.M33, vp.M44 - vp.M43).Normalized(),
        ];


        //Assume full visibility
        Visibility result = Visibility.FULL;
        Vector3 min = volume.MinPoint;
        Vector3 max = volume.MaxPoint;

        //Check all planes
        for (int i = 0; i < 6; i++)
        {
            Vector4 plane = planes[i];
            Vector3 normal = plane.Xyz;

            Vector3 maxPoint;
            maxPoint.X = normal.X >= 0 ? max.X : min.X;
            maxPoint.Y = normal.Y >= 0 ? max.Y : min.Y;
            maxPoint.Z = normal.Z >= 0 ? max.Z : min.Z;

            Vector3 minPoint;
            minPoint.X = normal.X >= 0 ? min.X : max.X;
            minPoint.Y = normal.Y >= 0 ? min.Y : max.Y;
            minPoint.Z = normal.Z >= 0 ? min.Z : max.Z;

            if (Vector3.Dot(normal, maxPoint) + plane.W < 0)
            {
                return Visibility.NONE;
            }

            if (Vector3.Dot(normal, minPoint) + plane.W < 0)
            {
                result = Visibility.PARTIAL;
            }
        }

        return result;
    }
}

public enum Visibility
{
    FULL,
    PARTIAL,
    NONE
}