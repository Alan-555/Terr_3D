using Terr3D.Server.Components;
using Terr3D.Server.Entities;
using Terr3D.Utils;

namespace Terr3D.Server.Engine;

public class ConcreteScene() : Scene()
{
    public override void SpawnDynamicEntities()
    {
        //Spawn player

        Diagnostics.Info("Initialising world...");


        var plr = Entity.Instantiate(() => new Player("Player", Worldspawn));

        World.ActiveCamera = plr.Children[0].GetComponent<Camera>();

        Diagnostics.Info("Scene init done");
    }

    public override void SpawnStaticEntities()
    {
        _ = new EnvConfig(EnvConfig.BuiltinSky);
        var terrain = new Terrain("Terrain", Worldspawn, 50f, 256, new(0, 0, 0));

        terrain.AddRegion(0, 0).Noise();
        terrain.AddRegion(-1,- 1).Noise();
        terrain.AddRegion(-1, 0).Noise();
        terrain.AddRegion(0, -1).Noise();

        terrain.Build();

        Entity.Instantiate(() => terrain);
    }
}