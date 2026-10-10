using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.Entities;
using Terr3D.Server.Shared;
using Terr3D.Utils;

namespace Terr3D.Server.Core;

public class ConcreteScene() : Scene()
{
    public override void SpawnDynamicEntities()
    {
        //Spawn player

        Diagnostics.Info("Initialising world...");


        var plr = Entity.Instantiate(() => new Player("Player", Worldspawn));

        World.ActiveCamera = plr.Children[0].GetComponent<Camera>();

        for (int i = 0; i < 100; i++)
        {
            var ent = new EmptyEntity($"test{i}", Worldspawn).WithComponents(new Renderer(Load<ShaderProgram>("shaders/surface/Shaded.frag.glsl"), Load<Mesh>("meshes/bambang.obj"), RendererClass.RENDERER_DYNAMIC)
            {
                material = new ShadedMaterial()
                {
                    diffuse = (1, 1, 1),
                    specular = (0.7f, 0, 0.8f),
                    shininess = 32,
                    albedo = Load<Texture>("textures/emptyWhite.png")


                }
            }, new Physics());
            ent.Transform.Position = RandHelper.RandomPointInUnitCircle3D(100f);
            Entity.Instantiate(() => ent);
        }

        Diagnostics.Info("Scene init done");
    }

    public override void SpawnStaticEntities()
    {
        new EnvConfig(EnvConfig.BuiltinSky).Register();
        var terrain = new Terrain("Terrain", Worldspawn, 50f, 256, new(0, 0, 0));
        for (int x = 0; x < 3; x++)
            for (int z = 0; z < 3; z++)
                terrain.AddRegion(x, z).Noise();

        terrain.Build();
        Entity.Instantiate(() => terrain);

        for (int i = 0; i < 100; i++)
        {
            /*var ent = new EmptyEntity($"test{i}", Worldspawn).WithComponent(new Renderer(ResourceManager.Shaders[ResourceIndex.Shaders.Shaded], ResourceManager.Meshes[ResourceIndex.Meshes.Bambang], RendererClass.RENDERER_STATIC)
            {
                material = new ShadedMaterial()
                {
                    diffuse = (1, 1, 1),
                    specular = (0.7f, 0, 0.8f),
                    shininess = 32


                }
            });
            ent.Transform.Position = RandHelper.RandomPointInUnitCircle3D(100f);
            Entity.Instantiate(() => ent);*/
        }
    }
}