using Terr3D.Server.Components;
using Terr3D.Server.Entities;
using Terr3D.Utils;

namespace Terr3D.Server.Engine;

public class ConcreteScene() : Scene(50f, 4)
{
    public override void SpawnDynamicEntities()
    {
        //Spawn player

        Diagnostics.Info("Initialising world...");
        
        
        var plr = Entity.Instantiate(()=> new Player("Player", Worldspawn));

        World.Instance.ActiveCamera = plr.Children[0].GetComponent<Camera>();

        Diagnostics.Info("Scene init done");
    }

    public override void SpawnStaticEntities()
    {
        var terrain = Entity.Instantiate(()=> new Terrain("Terrain", Worldspawn, 50f, 256));
    }
}