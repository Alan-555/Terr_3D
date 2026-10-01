using Terr3D.Server.Engine;
using Terr3D.Utils;

namespace Terr3D.Server.Entities;

public sealed class Worldspawn : Entity
{
    public static int WorldspawnIncremental = 0;
    private int _id = WorldspawnIncremental++;
    public new Entity Parent => null!;
    public override string Name => $"worldspawn{_id}";
    public override string FullName
    {
        get
        {
            if (Parent != null) return $"{Parent.FullName}.{Name}";
            else return Name;

        }
    }

    public Scene Scene {get; private init;}

    public Worldspawn(Scene scene) : base("worldspawn", null!, false)
    {
        Scene = scene;
    }
}