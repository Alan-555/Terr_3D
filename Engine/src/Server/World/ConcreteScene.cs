using Terr3D.Server.Entities;
using Terr3D.Utils;

namespace Terr3D.Server.World;

public class ConcreteScene : Scene
{
    public override void SpawnDynamicEntities()
    {
        //Spawn player

        Diagnostics.Info("Initialising world...");
        //Terrain worldSpawn = new("Terrain", Worldspawn, 64f, 100);
        Globals.CurrentCamera = Globals.Player.LocalPlayer.PlayerCam;
        Diagnostics.Info("Scene init done");
    }

    public override void SpawnStaticEntities()
    {
    }
}