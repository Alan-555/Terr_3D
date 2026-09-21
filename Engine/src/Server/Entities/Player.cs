using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.Shared;
using Terr3D.Server.World;
using Terr3D.Utils;

namespace Terr3D.Server.Entities;

/// <summary>
/// Represent the player
/// </summary>
public class Player : Entity, IPlayerProvider
{
    public Camera playerCamera;
    public FreeCamera freeCamera;
    public Physics physics;
    public Renderer worldModelMesh;

    float mouseSensitivity = 0.015f;

    float acceleration = 5;
    float moveSpeed = 9.1f; //assuming 1 doom map unit is one meter
    float sprintSpeedModifier = Program.DEBUG_FLAG ? 3 : 1.5f;

    bool isPlayerFrozen = false;

    public static bool debugCull = false;

    private float damage = 0f;

    const float criticalDamage = 5f;

    Renderer damageOverlayer;

    SmokeParticleSystem damageParticles;

    public bool RequestPickUp { get; private set; }
    public Camera PlayerCam => playerCamera;
    public Player LocalPlayer => this;

    EnvMusic _plrDamage;

    public Player(Scene scene, string name) : base(scene, name, false)
    {
        Register();
        //Spawn the camera pivot
        EmptyEntity cameraPivot = new(scene, "PlrNeck", false);
        cameraPivot.Transform.SetParent(Transform);
        cameraPivot.Transform.LocalPosition = new(0, 1.8f, 0);

        //Attach the camera component to the pivot
        playerCamera = cameraPivot.AddComponent<Camera>();

        //Spawn the mesh representation of the player
        EmptyEntity playerMesh = new(scene, "PlrWorldModel", false);
        playerMesh.Transform.SetParent(Transform);
        playerMesh.Transform.LocalRotation = Quaternion.FromEulerAngles(0, MathF.PI, 0);

        //Add a renderer to the mesh
        worldModelMesh = playerMesh.AddComponent(new Renderer(ResourceManager.Shaders[ResourceIndex.Shaders.Shaded], ResourceManager.Meshes[ResourceIndex.Meshes.Cube]));
        worldModelMesh.material = new ShadedMaterial()
        {
            diffuse = (1, 1, 1),
            specular = (0.7f, 0, 0.8f),
            shininess = 32


        };
        worldModelMesh.SetEnabled(false);

        //So player can move
        physics = AddComponent<Physics>();
        physics.Friction = 30;
        physics.Collider = AddComponent(new AABB_Collider(new Vector3(0.25f, 1.8f / 2f, 0.25f), new Vector3(0, 1.8f / 2f, 0)));

        //Free cam feature
        freeCamera = new FreeCamera(scene, "FreeCam");
        freeCamera.SetEnabled(false);

        damageOverlayer = AddComponent(new Renderer(ResourceManager.Shaders[ResourceIndex.Shaders.ScreenTexture], ResourceManager.Meshes[ResourceIndex.Meshes.Quad], RendererClass.RENDERER_PERSISTENT)
        {
            material = new ScreenTextureMaterial()
            {
                text = ResourceManager.Textures[ResourceIndex.Textures.EmptyWhite],
                pos = new(),
                colour = new(0.1f, 0, 0, 0)
            }
        });

        damageParticles =  AddComponent(new SmokeParticleSystem(new Vector4(141, 148, 95, 127) / 255f, 100, true));
        damageParticles.doEmit = false;
        damageParticles.Speed = 3.5f;
        damageParticles.EndScale = 5f;

        _plrDamage = new(scene, ResourceIndex.Audio.Damage)
        {
            MaxVolume = 1f
        };
    }

    

    public void Register()
    {
        Onstage.Globals.Register<IPlayerProvider>(this);
    }

    BoidBait? _heldBait;

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);

        CheckDamage(dt);

        UpdateSafety();

        //Debug player pos
        Onstage.Globals.Canvas.RenderLabel($"x|y|z {Transform}", 0);

        RequestPickUp = false;

        //General controls
        if (EngineWindow.Instance.KeyboardState.IsKeyPressed(Keys.F1))
        {
            ToggleFreeCam();
        }
        if (EngineWindow.Instance.KeyboardState.IsKeyPressed(Keys.F))
        {
            ToggleAttraction();
        }
        if (EngineWindow.Instance.IsKeyPressed(Keys.Escape))
        {
            EngineWindow.Instance.CursorState = CursorState.Normal;
        }
        else if (EngineWindow.Instance.IsMouseButtonDown(MouseButton.Button1))
        {
            RequestPickUp = true;
            EngineWindow.Instance.CursorState = CursorState.Grabbed;
        }
        else if (Program.DEBUG_FLAG && EngineWindow.Instance.IsKeyPressed(Keys.R))
        {

        }



        if (physics.Collider!.Intersects(new Volume_AABB(Onstage.Globals.CurrentCamera.Transform.Position, new(0.01f, 0.01f, 0.01f))))
            worldModelMesh.SetEnabled(false);
        else
            worldModelMesh.SetEnabled(true);


        //Other controls
        if (!isPlayerFrozen)
        {
            Move(dt);
            if (EngineWindow.Instance.MouseState.IsButtonPressed(MouseButton.Button2))
            {
                ThrowBait();
            }
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


    float _nextSafetyUpdate = 0.5f;

    public void UpdateSafety()
    {
        if (_heldBait == null) return;
        if (_nextSafetyUpdate <= EngineWindow.Time)
        {
            _nextSafetyUpdate = EngineWindow.Time + 0.5f;
        }
        else
        {
            return;
        }
        bool isSafe = Onstage.Partitioner.Raycast(new(Transform.Position, Vector3.UnitY), out _);

        _heldBait.ChangeStrength(isSafe);


    }


    public void DamagePlr(float dmg)
    {
        damage += dmg;
    }

    void CheckDamage(float dt)
    {
        Onstage.Globals.Canvas.RenderLabel($"Damage {damage}", 7);
        if (Onstage.Globals.Gameplay.ShouldBeDamaged(Transform.Position))
        {
            _plrDamage.FadeIn();
            damageParticles.doEmit = true;
            damage += dt * 0.7f;
        }
        else
        {
            _plrDamage.FadeOut();
            damageParticles.doEmit = false;
            if (damage > 0)
                damage = MathF.Max(0, damage - dt);
        }

        float t = damage / criticalDamage;

        ((ScreenTextureMaterial)damageOverlayer.material!).colour.W = t;

        if (t > 1f)
        {
            _ = PlrDead();
        }
    }
    bool shown = false;
    async Task PlrDead()
    {
        if (shown) return;
        EngineWindow.Instance.IsPaused = true;
        shown = true;
        await NativeDialogue.ShowMessageAsync($"You have decayed into nothing.\nYou saved {Onstage.Globals.Worldspawn.BoidManager.RescuedBoids}/{Onstage.Globals.Worldspawn.BoidManager.TotalBoids} dragons.");
        EngineWindow.Instance.Close();
    }

    void ToggleAttraction()
    {
        if (_heldBait == null)
        {
            _heldBait = new BoidBait(Onstage, "HeldBait", true);
            _heldBait.Transform.SetParent(Transform);
            _heldBait.Transform.LocalPosition = new Vector3(0, 0f, 0);
            _heldBait.StartAttracting(false);
        }
        else
        {
            _heldBait.Cleanup();
            _heldBait.Destroy();
            _heldBait = null;
        }
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
    BoidBait[] _baits = new BoidBait[3];
    int _boidPtr = 0;
    void ThrowBait()
    {
        BoidBait bait = new(Onstage, "");
        _baits[_boidPtr]?.Cleanup();
        _baits[_boidPtr]?.Destroy();
        _baits[_boidPtr] = bait;
        _boidPtr = (_boidPtr + 1) % _baits.Length;
        bait.Transform.Position = playerCamera.Transform.Position;

        // Push in camera forward direction
        bait.GetComponent<Physics>()?.Push(playerCamera.Transform.Forward * 15f + physics.Velocity);
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