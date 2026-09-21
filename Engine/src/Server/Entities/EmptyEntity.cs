using OpenTK.Mathematics;
using Terr3D.Server.World;

namespace Terr3D.Server.Entities;

/// <summary>
/// An empty entity which can be instantiated
/// </summary>
public class EmptyEntity : Entity
{
    public EmptyEntity(Scene scene, string name, bool isStatic) : base(scene, name, isStatic)
    {
    }
}


/// <summary>
/// Terrain entity also stores its min and max height
/// </summary>
public class TerrainEntity : EmptyEntity
{
    public float maxHeight, minHeight;
    public TerrainEntity(Scene scene, string name, Vector2 heightData) : base(scene, name, true)
    {
        minHeight = heightData.X;
        maxHeight = heightData.Y;
    }
}