using OpenTK.Mathematics;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.World;

namespace Terr3D.Server.Entities;

/// <summary>
/// The debug entity allows to render a dynamic model
/// </summary>
public class DebugEntity : Entity
{
    Renderer renderer;
    bool doRotate = true;

    public DebugEntity(string name, Entity parent, Mesh mesh, bool doRotate = true) : base(name, parent, false)
    {
        this.doRotate = doRotate;

        renderer = AddComponent(new Renderer(ResourceManager.Shaders[ResourceIndex.Shaders.Shaded], mesh));
    }
    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);
        if(doRotate)
            Transform.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, dt) * Transform.Rotation;
    }
}