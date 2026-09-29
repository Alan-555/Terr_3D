using OpenTK.Mathematics;

namespace Terr3D.Server.Components;

public sealed class GroundService
{
    private readonly List<IGroundProvider> _groundProviders = [];
    internal void Register(IGroundProvider provider) => _groundProviders.Add(provider);
    internal void Unregister(IGroundProvider provider) => _groundProviders.Remove(provider);

    public float GetHeightAt(Vector3 v) => GetHeightAt(v.X, v.Z);
    public float GetHeightAt(Vector2 v) => GetHeightAt(v.X, v.Y);

    public float GetHeightAt(float x, float z)
    {
        if(_groundProviders.Count == 0) throw new InvalidOperationException("There is no ground to stand on!");
        IGroundProvider closest = null!;
        float closestDst = float.PositiveInfinity;

        foreach (var p in _groundProviders)
        {
            if(p.Contains(x, z))
            {
                return p.GetHeightAt(x, z);
            }
            var dst = Vector2.Distance(p.GetOrigin(), new(x,z));
            if(dst < closestDst)
            {
                closestDst = dst;
                closest = p;
            }
        }

        return closest.GetHeightAt(x, z);
    }

    public Vector3 GetNormalAt(Vector3 v) => GetNormalAt(v.X, v.Z);
    public Vector3 GetNormalAt(Vector2 v) => GetNormalAt(v.X, v.Y);
    public Vector3 GetNormalAt(float x, float z)
    {
        if(_groundProviders.Count == 0) throw new InvalidOperationException("There is no ground to stand on!");
        IGroundProvider closest = null!;
        float closestDst = float.PositiveInfinity;

        foreach (var p in _groundProviders)
        {
            if(p.Contains(x, z))
            {
                return p.GetNormalAt(x, z);
            }
            var dst = Vector2.Distance(p.GetOrigin(), new(x,z));
            if(dst < closestDst)
            {
                closestDst = dst;
                closest = p;
            }
        }

        return closest.GetNormalAt(x, z);
    }
}

public interface IGroundProvider
{
    bool Contains(float x, float z);
    public float GetHeightAt(float x, float z);
    public Vector3 GetNormalAt(float x, float z);
    public Vector2 GetOrigin();
}

public static class GlobalGroundProviderExtensions
{
    public static bool Contains(this IGroundProvider provider, Vector3 pos) 
        => provider.Contains(pos.X, pos.Z);

    public static bool Contains(this IGroundProvider provider, Vector2 pos) 
        => provider.Contains(pos.X, pos.Y);
    public static float GetHeightAt(this IGroundProvider provider, Vector3 pos) 
        => provider.GetHeightAt(pos.X, pos.Z);

    public static float GetHeightAt(this IGroundProvider provider, Vector2 pos) 
        => provider.GetHeightAt(pos.X, pos.Y);

    public static Vector3 GetNormalAt(this IGroundProvider provider, Vector3 pos) 
        => provider.GetNormalAt(pos.X, pos.Z);

    public static Vector3 GetNormalAt(this IGroundProvider provider, Vector2 pos) 
        => provider.GetNormalAt(pos.X, pos.Y);
}