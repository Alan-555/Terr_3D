using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Terr3D.Client.Resources;
using Terr3D.Server.Entities;
using Terr3D.Server.Core;
using Terr3D.Utils;
using ImGuiNET;
using Terr3D.Client.Utils;

namespace Terr3D.Client;

/// <summary>
/// This class controls the screen of the game. Uses a singleton access pattern
/// </summary>
public class EngineWindow : GameWindow
{

    public static EngineWindow Instance => _instance ?? throw new Exception("The screen was not initialised!");
    static EngineWindow? _instance;

    private ImGuiController? _controller;

    public DateTime _startTime = DateTime.Now;
    

    /// <summary>
    /// Returns the total elapsed time, since the start of the engine
    /// </summary>
    public static float Time => (float)(DateTime.Now - Instance._startTime).TotalSeconds;

    public static int TimeMillis => (int)(DateTime.Now - Instance._startTime).TotalMilliseconds;


    public bool IsPaused { get; set; }
    public float TimeScale { get; set; } = 1f;
    public bool IsPresentUI {get; set;} = false;

    /// <summary>
    /// The scene drawer that draws the scene
    /// </summary>
    public WorldDrawer? SceneDrawer { get; private set; }


    /// <summary>
    /// An action fired once the GPU assets are ready
    /// </summary>
    Action? OnGpuContextReady;

    private EngineWindow(NativeWindowSettings windowSettings) : base(GameWindowSettings.Default, windowSettings)
    {
        _instance = this;
    }

    public static EngineWindow ConstructScreen(Action onGpuContextReady)
    {
        var nativeWindowSettings = new NativeWindowSettings()
        {
            ClientSize = new Vector2i(1080, 720),
            Title = "Terr3D",
            Profile = ContextProfile.Core,
            APIVersion = new Version(3, 3),
            Flags = ContextFlags.ForwardCompatible,
            DepthBits = 24,
            StartVisible = false,
        };

        return new EngineWindow(nativeWindowSettings)
        {
            OnGpuContextReady = onGpuContextReady
        };
    }


    protected override void OnLoad()
    {
        base.OnLoad();

        _controller = new ImGuiController(ClientSize.X, ClientSize.Y);

        //Initialises GL
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);

        //Now with resources loaded, the GPU is ready on the application level
        OnGpuContextReady?.Invoke();
    }


    float _fpsUpdateTimer = 1;
    float FPS = 0;
    Queue<float> _frameTimes = new System.Collections.Generic.Queue<float>();
    const int MaxFrameSamples = 60;

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        //clear the buffers
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        //render the world
        World.DrawWorld();

        

        _controller?.Render();

        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        
        _controller?.Update(this, (float)args.Time);

        if (IsPresentUI)
            CursorState = CursorState.Normal;
        else
            CursorState = CursorState.Grabbed;
        

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
        World.EngineLoop(IsPaused ? 0 : dt * TimeScale);

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
        _controller?.WindowResized(e.Width, e.Height);
        //also update the current camera
        World.ActiveCamera?.UpdateAspect(ClientSize.X, ClientSize.Y);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _controller?.PressChar((char)e.Unicode);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _controller?.MouseScroll(e.Offset);
    }

    protected override void OnUnload()
    {
        _controller?.Dispose();
        World.DestroyWorld();
        base.OnUnload();
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        _controller?.AddKeyEvent(e.Key, true);
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs e)
    {
        base.OnKeyUp(e);
        _controller?.AddKeyEvent(e.Key, false);
    }
}
