using OpenTK.Mathematics;

namespace Terr3D.Server.Shared;

public static class RandHelper
{
    public static Random rand = new(DateTime.Now.Millisecond);

    public static float RandFloat(float min, float max)
    {
        return Random.Shared.NextSingle() * (max - min) + min;
    }
    public static float RandFloatNormalised()
    {
        return Random.Shared.NextSingle();
    }

    public static Vector2 RandomPointInUnitCircle(float radius)
    {
        float angle = RandFloatNormalised() * MathF.PI * 2;
        float dirX = MathF.Cos(angle);
        float dirZ = MathF.Sin(angle);

        float uniformRadius = MathF.Sqrt(RandFloatNormalised()) * radius;
        return new(dirX * uniformRadius, dirZ * uniformRadius);
    }

    public static Vector3 RandomPointInUnitCircle3D(float radius)
    {
        var pos = RandomPointInUnitCircle(radius);
        return new(pos.X, 0, pos.Y);
    }

}