using Terr3D.Client;
using Terr3D.Server.World;
using Terr3D.Utils;

namespace Terr3D;
class Program
{
    public static bool DEBUG_FLAG { get; private set; }
    static void Main(string[] args)
    {
        if (args.Contains("--debug"))
        {
            DEBUG_FLAG = true;
        }
        Diagnostics.Info("Initialising client...");
        //init the client window
        InitClient(()=>InitRest(true));
    }

    static void InitClient(Action OnGpuReady)
    {
        if(DEBUG_FLAG)
            Profiler.Initialise();
        var screen = Client.EngineWindow.ConstructScreen(OnGpuReady);
        screen.Run();
    }

    static void InitRest(bool isMenu)
    {
        Scene scene = new();

        //once client and GL context is ready, we initialise the rest
        scene.InitWorld();
        if(isMenu)
            scene.InitMainMenu();

        EngineWindow.Instance.ChangeScene(scene);
        Diagnostics.Info("Loading done.");
        EngineWindow.Instance.IsVisible = true;
    }

    public static void NewScene()
    {
        InitRest(false);
    }
}