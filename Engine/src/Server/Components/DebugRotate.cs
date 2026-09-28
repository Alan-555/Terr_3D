using OpenTK.Mathematics;
using Terr3D.Server.Shared;

namespace Terr3D.Server.Components;

public class DebugRotate : BehaviourComponent
{
    protected override void OnUpdate(float dt)
    {
        Transform.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, dt) * Transform.Rotation;
    }
}