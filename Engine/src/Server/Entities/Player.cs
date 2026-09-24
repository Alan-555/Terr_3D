using OpenTK.Mathematics;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;

namespace Terr3D.Server.Entities;

public class Player : Entity
{
    public Player(string name, Entity parent) : base(name, parent, false)
    {
        //Spawn the camera pivot
        var cameraPivot = new EmptyEntity("PlrNeck", this).WithComponent<Camera>();
        cameraPivot.Transform.LocalPosition = new(0, 1.8f, 0);

        //Spawn the mesh representation of the player
        var playerMesh =
        new EmptyEntity("PlrWorldModel", this)
        .WithComponent(new Renderer(ResourceManager.Shaders[ResourceIndex.Shaders.Shaded], ResourceManager.Meshes[ResourceIndex.Meshes.Cube])
        {
            material = new ShadedMaterial()
            {
                diffuse = (1, 1, 1),
                specular = (0.7f, 0, 0.8f),
                shininess = 32


            }
        });
        playerMesh.Transform.LocalRotation = Quaternion.FromEulerAngles(0, MathF.PI, 0);


        playerMesh.SetEnabled(false);

        //So player can move
        AddComponent(new Physics()
        {
            Friction = 30f,
            Collider = AddComponent(new AABB_Collider(new Vector3(0.25f, 1.8f / 2f, 0.25f), new Vector3(0, 1.8f / 2f, 0)))
        });

        //Free cam feature
        var freeCamEntity = InstantiateEmpty("FreeCamera", Onstage.Worldspawn).WithComponent<Camera>();
        var fcc = freeCamEntity.AddComponent<FreeCameraController>();
        freeCamEntity.SetEnabled(false);

        AddComponent<PlayerController>().freeCamera = fcc;
    }
}