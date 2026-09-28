using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Terr3D.Client;
using Terr3D.Server.Components;
using Terr3D.Server.Engine;

namespace Terr3D.Server.Components;

/// <summary>
/// A camera with fly controls
/// </summary>
public class FreeCameraController : BehaviourComponent
{
    [Dependency]
    public Camera camera = null!;

    float mouseSensitivity = 0.015f;

    float moveSpeed = 3;
    float sprintSpeedModifier = 10;


    protected override void OnUpdate(float dt)
    {
        Move(dt);
        Turn(dt);

    }

    void Move(float dt)
    {
        Vector3 move = new();
        if (Input.KeyDown(KeyCode.S))
            move.Z = 1;
        if (Input.KeyDown(KeyCode.W))
            move.Z = -1;
        if (Input.KeyDown(KeyCode.A))
            move.X = -1;
        if (Input.KeyDown(KeyCode.D))
            move.X = 1;
        if (Input.KeyDown(KeyCode.Space))
            move.Y = 1;
        if (Input.KeyDown(KeyCode.C))
            move.Y = -1;
        var localRight = new Vector4(1, 0, 0, 0) * Transform.GlobalMatrix;
        var localUp = new Vector4(0, 1, 0, 0) * Transform.GlobalMatrix;
        var localForward = new Vector4(0, 0, 1, 0) * Transform.GlobalMatrix;

        var moveRight = move.X * localRight.Xyz;
        var moveUp = move.Y * localUp.Xyz;
        var moveForward = move.Z * localForward.Xyz;

        float speed = EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.LeftShift) ? moveSpeed * sprintSpeedModifier : moveSpeed;

        Transform.Position += (moveRight + moveUp + moveForward) * dt * speed;
    }

    void Turn(float dt)
    {

        Vector3 rotateInput = new();
        var delta = Input.MouseDelta;
        rotateInput.X -= delta.Y;
        rotateInput.Y -= delta.X;

        Quaternion yaw = Quaternion.FromAxisAngle(Vector3.UnitY, rotateInput.Y * mouseSensitivity);
        Quaternion pitch = Quaternion.FromAxisAngle(Vector3.UnitX, rotateInput.X * mouseSensitivity);


        Transform.Rotation = yaw * Transform.Rotation * pitch;

        Transform.Rotation.Normalize();
    }
}