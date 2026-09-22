using Terr3D.Server.World;
using Terr3D.Utils;

namespace Terr3D.Server.Entities;

public class Worldspawn : Entity
{
    new Entity Parent => null!;

    public Worldspawn(Scene scene) : base("worldspawn", false)
    {
        //Spawn canvas
        new Canvas(this, "Canvas");

        //Prepare the space partitioner
        Diagnostics.Info("Generating partitioning data...");
        Partitioner = new(worldSpawn);
    }
}