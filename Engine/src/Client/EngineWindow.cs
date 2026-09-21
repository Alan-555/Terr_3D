using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Terr3D.Client.Resources;
using Terr3D.Server.Entities;
using Terr3D.Server.World;
using Terr3D.Utils;

namespace Terr3D.Client;

/// <summary>
/// This class controls the screen of the game. Uses a singleton access pattern
/// </summary>
public class EngineWindow : GameWindow
{

    public static EngineWindow Instance => _instance ?? throw new Exception("The screen was not initialised!");
    static EngineWindow? _instance;

    public DateTime _startTime = DateTime.Now;

    /// <summary>
    /// Returns the total elapsed time, since the start of the engine
    /// </summary>
    public static float Time => (float)(DateTime.Now - Instance._startTime).TotalSeconds;

    public static int TimeMillis => (int)(DateTime.Now - Instance._startTime).TotalMilliseconds;


    public bool IsPaused { get; set; }
    public float TimeScale { get; set; } = 1f;

    /// <summary>
    /// The scene this window is to render
    /// </summary>
    public Scene? BoundScene { get; private set; }

    /// <summary>
    /// The scene drawer that draws the scene
    /// </summary>
    public SceneDrawer? SceneDrawer { get; private set; }


    /// <summary>
    /// An action fired once the GPU assets are ready
    /// </summary>
    Action? OnGpuReady;

    private EngineWindow(NativeWindowSettings windowSettings) : base(GameWindowSettings.Default, windowSettings)
    {
        _instance = this;
    }

    public void ChangeScene(Scene scene)
    {
        BoundScene = scene;
        SceneDrawer = new(scene);
    }

    public static EngineWindow ConstructScreen(Action onGpuReady)
    {
        var nativeWindowSettings = new NativeWindowSettings()
        {
            ClientSize = new Vector2i(800, 600),
            Title = "Terr3D",
            Profile = ContextProfile.Core,
            APIVersion = new Version(3, 3),
            Flags = ContextFlags.ForwardCompatible,
            DepthBits = 24,
            StartVisible = false,
        };

        return new EngineWindow(nativeWindowSettings)
        {
            OnGpuReady = onGpuReady
        };
    }


    protected override void OnLoad()
    {
        base.OnLoad();

        //Initialises GL
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);

        //Now that GL is ready, we can load the GPU resources
        ResourceManager.InitialiseGPUResources();

        //Now with resources loaded, the GPU is ready on the application level
        OnGpuReady?.Invoke();
    }


    float _fpsUpdateTimer = 1;
    float FPS = 0;
    Queue<float> _frameTimes = new System.Collections.Generic.Queue<float>();
    const int MaxFrameSamples = 60;

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        Profiler.Start(ProfilerPhase.FRAME_PREPARE);

        //Clear buffers
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        //Render the scene
        SceneDrawer?.RenderScene();

        SwapBuffers();

        Profiler.EndAll();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        Profiler.Start(ProfilerPhase.ROOT);
        Profiler.Start(ProfilerPhase.SCENE_UPDATE);

        float dt = MathF.Min((float)args.Time, 1); //cap the time to avoid overshooting. At that point. the game is maxing at 1 FPS anyway...

        //calculate the framerate
        _frameTimes.Enqueue(dt);
        if (_frameTimes.Count > MaxFrameSamples)
        {
            _frameTimes.Dequeue();
        }

        float averageDt = 0;
        foreach (float time in _frameTimes)
        {
            averageDt += time;
        }
        averageDt /= _frameTimes.Count;
        FPS = 1f / averageDt;

        //do we update now?
        _fpsUpdateTimer -= dt;
        if (_fpsUpdateTimer <= 0)
        {
            _fpsUpdateTimer = 1;
            Title = $"Terr3D | FPS: {MathF.Round(FPS)}";
        }

        Controls();

        //update scene
        BoundScene?.UpdateScene(IsPaused ? 0 : dt * TimeScale);

    }

    void Controls()
    {
        if (KeyboardState.IsKeyPressed(Keys.Pause))
        {
            IsPaused = !IsPaused;
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        //also update the current camera
        BoundScene?.Globals.CurrentCamera.UpdateAspect(ClientSize.X, ClientSize.Y);
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        Diagnostics.Info("Engine clean up...");
        BoundScene?.DestroyScene();
    }
}
