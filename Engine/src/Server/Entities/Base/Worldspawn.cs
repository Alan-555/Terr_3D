using Terr3D.Server.Core;
using Terr3D.Utils;

namespace Terr3D.Server.Entities;

public sealed class Worldspawn : Entity
{
    public new Entity Parent => null!;
    public override string Name => $"worldspawn{Scene.Id}";
    public override string FullName
    {
        get
        {
            if (Parent != null) return $"{Parent.FullName}.{Name}";
            else return Name;

        }
    }

    public Scene Scene {get; private init;}

    public Worldspawn(Scene scene) : base("worldspawn", null!)
    {
        Scene = scene;
    }
}