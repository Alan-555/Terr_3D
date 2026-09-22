using Terr3D.Server.World;
using Terr3D.Utils;

namespace Terr3D.Server.Entities;

public class Worldspawn : Entity
{
    public new Entity Parent => null!;

    public Scene Scene {get; private init;}

    public Worldspawn(Scene scene) : base("worldspawn", null!, false)
    {
        Scene = scene;
    }
}