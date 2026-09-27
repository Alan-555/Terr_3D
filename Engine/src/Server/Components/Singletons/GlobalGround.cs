using OpenTK.Mathematics;

namespace Terr3D.Server.Components;

public class GlobalGround(IGlobalGroundProvider provider) : SingletonComponent(), IGlobalGroundProvider
{
    public IGlobalGroundProvider Provider {get; set;} = provider;

    public float GetHeightAt(float x, float z) => Provider.GetHeightAt(x, z);

    public Vector3 GetNormalAt(float x, float z) => Provider.GetNormalAt(x, z);
}


public interface IGlobalGroundProvider
{
    public float GetHeightAt(float x, float z);
    public Vector3 GetNormalAt(float x, float z);
}

public static class GlobalGroundProviderExtensions
{
    public static float GetHeightAt(this IGlobalGroundProvider provider, Vector3 pos) 
        => provider.GetHeightAt(pos.X, pos.Z);

    public static float GetHeightAt(this IGlobalGroundProvider provider, Vector2 pos) 
        => provider.GetHeightAt(pos.X, pos.Y);

    public static Vector3 GetNormalAt(this IGlobalGroundProvider provider, Vector3 pos) 
        => provider.GetNormalAt(pos.X, pos.Z);

    public static Vector3 GetNormalAt(this IGlobalGroundProvider provider, Vector2 pos) 
        => provider.GetNormalAt(pos.X, pos.Y);
}