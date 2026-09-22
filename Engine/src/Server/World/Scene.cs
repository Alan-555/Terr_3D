using OpenTK.Mathematics;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.Entities;
using Terr3D.Server.Shared;
using Terr3D.Utils;

namespace Terr3D.Server.World;

public abstract class Scene
{
    public Worldspawn Worldspawn;
    public SceneRegistry SceneRegistry { get; private set; } = new();
    public SceneGlobals Globals { get; private set; } = new();

    public SpacePartitioner Partitioner { get; private set; }

    public Scene()
    {
        
        Worldspawn = new(this);
        new Player(this, "Player");
        
        SpawnStaticEntities();
        Partitioner = new(null);
        SpawnDynamicEntities();
    }
    
    public abstract void SpawnStaticEntities();
    public abstract void SpawnDynamicEntities();

    /// <summary>
    /// Initialises the world
    /// </summary>
    public void InitWorld()
    {
        //Spawn player

        Diagnostics.Info("Initialising world...");
        Terrain worldSpawn = new(this, "worldspawn", "res/textures/skala.png");

        




        Globals.CurrentCamera = Globals.Player.LocalPlayer.PlayerCam;


        Diagnostics.Info("Scene init done");
    }

    public void InitMainMenu()
    {
        Globals.Player.LocalPlayer.Transform.Position = new(1000, 1000, 1000);
        Globals.Player.LocalPlayer.SetEnabled(false);
        Globals.CurrentCamera.Transform.Position = new(-3, 13, 14);
        Globals.CurrentCamera.Transform.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, 45 * MathHelper.DegToRad);

        new LogicMainMenu(this);
    }

    public void InitDebugScene()
    {
        Diagnostics.Info("Spawning debug entities...");


        /*DebugEntity dragon = new(this, "dragon", ResourceManager.Meshes.majesticDragon);
        dragon.Transform.Position = new Vector3(0, 0, -1);
        //dragon.AddComponent<Physics>();

        DebugEntity tea = new(this, "tea", ResourceManager.Meshes.tea, false);
        tea.Transform.Position = new Vector3(5, 0, 0);
        tea.Transform.Rotation = Quaternion.FromEulerAngles(0, -90, 0);
        tea.Transform.SetParent(dragon.Transform);

        for (int i = 0; i < 100; i++)
        {
            DebugEntity bb = new(this, "Bambang", ResourceManager.Meshes.bambang);
            bb.Transform.Position = new Vector3(Random.Shared.Next(0, 16), 100, Random.Shared.Next(0, 16));
            bb.AddComponent<Physics>();
        }*/
    }

    /// <summary>
    /// Updates all enabled entities and their components
    /// </summary>
    /// <param name="dt">The deltatime</param>
    public void UpdateScene(float dt)
    {
        SceneRegistry.BeginUpdate();

        //iterate all updatable objects
        foreach (var updatable in SceneRegistry.UpdatableObjects)
        {
            //check if the subject is enabled
            if (updatable is Entity { IsEnabled: false } or Entity { IsDestroyed: true }) continue;
            if (updatable is Component { IsEnabled: false } or Component { IsDestroyed: true }) continue;

            updatable.OnUpdate(dt);
        }

        SceneRegistry.EndUpdate();
    }



    public void DestroyScene()
    {
        Entity[] tempE = new Entity[SceneRegistry.Entities.Count];
        SceneRegistry.Entities.CopyTo(tempE);
        foreach (var ent in tempE)
        {
            ent.Destroy();
        }

    }
}