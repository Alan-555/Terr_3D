using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Terr3D.Client;
using Terr3D.Server.Components;
using Terr3D.Server.World;

namespace Terr3D.Server.Entities;

/// <summary>
/// A camera with fly controls
/// </summary>
public class FreeCamera : Entity
{
    public Camera camera;

    float mouseSensitivity = 0.015f;

    float moveSpeed = 3;
    float sprintSpeedModifier = 10;

    public FreeCamera(Scene scene, string name) : base(scene, name, false)
    {
        camera = AddComponent<Camera>();
    }

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);
        Move(dt);
        Turn(dt);

    }

    void Move(float dt)
    {
        Vector3 move = new();
        if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.S))
            move.Z = 1;
        if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.W))
            move.Z = -1;
        if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.A))
            move.X = -1;
        if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.D))
            move.X = 1;
        if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.Space))
            move.Y = 1;
        if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.C))
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
        var delta = EngineWindow.Instance.MouseState.Delta;
        rotateInput.X -= delta.Y;
        rotateInput.Y -= delta.X;

        Quaternion yaw = Quaternion.FromAxisAngle(Vector3.UnitY, rotateInput.Y * mouseSensitivity);
        Quaternion pitch = Quaternion.FromAxisAngle(Vector3.UnitX, rotateInput.X * mouseSensitivity);


        Transform.Rotation = yaw * Transform.Rotation * pitch;

        Transform.Rotation.Normalize();
    }
}