using OpenTK.Mathematics;
using Terr3D.Server.World;

namespace Terr3D.Server.Entities;

/// <summary>
/// An empty entity which can be instantiated
/// </summary>
public class EmptyEntity : Entity
{
    public EmptyEntity(string name, Entity parent, bool isStatic = false) : base(name, parent, isStatic)
    {
    }
}


/// <summary>
/// Terrain entity also stores its min and max height
/// </summary>
public class TerrainEntity : EmptyEntity
{
    public float maxHeight, minHeight;
    public TerrainEntity(string name, Entity parent, Vector2 heightData) : base(name, parent, true)
    {
        minHeight = heightData.X;
        maxHeight = heightData.Y;
    }
}