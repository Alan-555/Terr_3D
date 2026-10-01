using Terr3D.Client;
using Terr3D.Server.Engine;
using Terr3D.Utils;

namespace Terr3D;
class Program
{
    public static bool DEBUG_FLAG { get; private set; }
    static void Main(string[] args)
    {
        if (args.Contains("--debug"))
        {
            Diagnostics.Debug("Debug mode enabled");
            DEBUG_FLAG = true;
        }
        Diagnostics.Info("Initialising client...");
        //init the client window
        InitClient(()=>InitRest(false));
    }

    static void InitClient(Action OnResourcesReady)
    {
        if(DEBUG_FLAG)
            Profiler.Initialise();
        var screen = Client.EngineWindow.ConstructScreen(OnResourcesReady);
        screen.Run();
    }

    static void InitRest(bool isMenu)
    {
        World.Create();
        Diagnostics.Info("World crated. Spawning scene...");
        Scene scene = new ConcreteScene();
        World.LoadScene(scene);
        EngineWindow.Instance.IsVisible = true;
    }
}