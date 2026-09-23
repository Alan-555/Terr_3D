using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.Shared;
using Terr3D.Server.World;
using Terr3D.Utils;

namespace Terr3D.Server.Components;

/// <summary>
/// Represent the player
/// </summary>
public class PlayerController : BehaviourComponent
{
    
    [Dependency]
    public Camera playerCamera;
    [Dependency]
    public FreeCameraController freeCamera;

    [Dependency]
    public Physics physics;

    [Dependency]
    public Renderer worldModelMesh;

    float mouseSensitivity = 0.015f;

    float acceleration = 5;
    float moveSpeed = 9.1f; //assuming 1 doom map unit is one meter
    float sprintSpeedModifier = Program.DEBUG_FLAG ? 3 : 1.5f;

    bool isPlayerFrozen = false;

    public static bool debugCull = false;

    public Camera PlayerCam => playerCamera;
    public PlayerController LocalPlayer => this;


    public override void Update(float dt)
    {
        //Debug player pos
        Onstage.Globals.Canvas.RenderLabel($"x|y|z {Transform}", 0);

        //General controls
        if (EngineWindow.Instance.KeyboardState.IsKeyPressed(Keys.F1))
        {
            ToggleFreeCam();
        }
        if (EngineWindow.Instance.IsKeyPressed(Keys.Escape))
        {
            EngineWindow.Instance.CursorState = CursorState.Normal;
        }



        if (physics.Collider!.Intersects(new Volume_AABB(Onstage.Globals.CurrentCamera.Transform.Position, new(0.01f, 0.01f, 0.01f))))
            worldModelMesh.SetEnabled(false);
        else
            worldModelMesh.SetEnabled(true);


        //Other controls
        if (!isPlayerFrozen)
        {
            Move(dt);
        }
        if (!isPlayerFrozen || debugCull)
            Turn(dt);

    }

    public bool Raycast(out RaycastHit hit)
    {
        if (Onstage.Partitioner.Raycast(new(playerCamera.Transform.Position, playerCamera.Transform.Forward), out hit))
        {
            return true;
        }
        return false;
    }



    void ToggleFreeCam()
    {

        //Which camera to enable?
        bool toEnableFreeCam = Onstage.Globals.CurrentCamera == playerCamera;
        var newCam = toEnableFreeCam ? freeCamera.camera : playerCamera;
        Onstage.Globals.CurrentCamera = newCam;
        if (toEnableFreeCam)
        {
            playerCamera.SetEnabled(false);
            freeCamera.SetEnabled(true);
            isPlayerFrozen = true;
            if (!EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.LeftControl))
            {
                freeCamera.Transform.Position = playerCamera.Transform.Position;
                freeCamera.Transform.Rotation = playerCamera.Transform.Rotation;
            }
        }
        else
        {
            if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.LeftControl))
            {
                Transform.Position = freeCamera.Transform.Position;
            }
            playerCamera.SetEnabled(true);
            freeCamera.SetEnabled(false);
            isPlayerFrozen = false;
        }
    }

    void Move(float dt)
    {
        //read keys
        Vector3 move = new();
        if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.S))
            move.Z += 1;
        if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.W))
            move.Z += -1;
        if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.A))
            move.X += -1;
        if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.D))
            move.X += 1;
        if (EngineWindow.Instance.KeyboardState.IsKeyPressed(Keys.Space))
            RequestJump(dt);

        //transform movement vectors
        var localRight = new Vector4(1, 0, 0, 0) * Transform.GlobalMatrix;
        var localForward = new Vector4(0, 0, 1, 0) * Transform.GlobalMatrix;

        var moveRight = move.X * localRight.Xyz;
        var moveForward = move.Z * localForward.Xyz;

        //calculate the desired speed and direction
        float wishSpeed = EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.LeftShift) ? moveSpeed * sprintSpeedModifier : moveSpeed;
        var wishDir = moveRight + moveForward;
        if (wishDir.Length == 0)
        {
            return;
        }
        //normalise the direction
        wishDir.Normalize();


        Accelerate(wishDir, wishSpeed, dt);
    }

    void RequestJump(float dt)
    {
        if (physics.IsGrounded)
            physics.Push(new Vector3(0, 5f, 0));
    }


    void Accelerate(Vector3 wishDir, float wishSpeed, float dt)
    {
        //check how much the wishDir matches current velocity
        float actualWishDirSize = Vector3.Dot(physics.Velocity, wishDir);

        float speedToAdd = wishSpeed - actualWishDirSize;

        if (speedToAdd <= 0) return;

        //calculate the acceleration speed based on how much speed remain until a cap is reached
        float accelSpeed = MathF.Min(acceleration * dt * wishSpeed, speedToAdd);

        //actually accelerate
        physics.Push(new(wishDir.X * accelSpeed, 0, wishDir.Z * accelSpeed));
    }

    void Turn(float dt)
    {

        //read mouse
        Vector3 rotateInput = new();
        var delta = EngineWindow.Instance.MouseState.Delta;
        rotateInput.X = -delta.Y;
        rotateInput.Y = -delta.X;

        //calculate rotation quartations
        Quaternion yaw = Quaternion.FromAxisAngle(Vector3.UnitY, rotateInput.Y * mouseSensitivity);
        Quaternion pitch = Quaternion.FromAxisAngle(Vector3.UnitX, rotateInput.X * mouseSensitivity);

        //rotate player and the camera
        Transform.Rotation = yaw * Transform.Rotation;
        playerCamera.Transform.Rotation = playerCamera.Transform.Rotation * pitch;
        float pitchEuler = playerCamera.Transform.LocalRotation.ToEulerAngles().X;
        pitchEuler = Math.Clamp(pitchEuler, -MathF.PI / 2f, MathF.PI / 2f);
        playerCamera.Transform.LocalRotation = Quaternion.FromEulerAngles(pitchEuler, 0, 0);

        //a quartation should be normalised
        Transform.Rotation.Normalize();
        playerCamera.Transform.Rotation.Normalize();
    }
}