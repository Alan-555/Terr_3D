using OpenTK.Mathematics;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;

namespace Terr3D.Server.Entities;

public class Player : Entity
{
    public Player(string name, Entity parent) : base(name, parent, false)
    {
        AddComponent<PlayerController>();
        //Spawn the camera pivot
        EmptyEntity cameraPivot = InstantiateEmpty("PlrNeck", this);
        cameraPivot.Transform.LocalPosition = new(0, 1.8f, 0);

        //Attach the camera component to the pivot
        cameraPivot.AddComponent<Camera>();

        //Spawn the mesh representation of the player
        EmptyEntity playerMesh = InstantiateEmpty("PlrWorldModel", this);
        playerMesh.Transform.LocalRotation = Quaternion.FromEulerAngles(0, MathF.PI, 0);

        //Add a renderer to the mesh
        var worldModelMesh = playerMesh.AddComponent(new Renderer(ResourceManager.Shaders[ResourceIndex.Shaders.Shaded], ResourceManager.Meshes[ResourceIndex.Meshes.Cube]));
        worldModelMesh.material = new ShadedMaterial()
        {
            diffuse = (1, 1, 1),
            specular = (0.7f, 0, 0.8f),
            shininess = 32


        };
        worldModelMesh.SetEnabled(false);

        //So player can move
        var physics = AddComponent<Physics>();
        physics.Friction = 30;
        physics.Collider = AddComponent(new AABB_Collider(new Vector3(0.25f, 1.8f / 2f, 0.25f), new Vector3(0, 1.8f / 2f, 0)));

        //Free cam feature
        var freeCamera = AddComponent<FreeCameraController>();
        freeCamera.SetEnabled(false);
    }
}