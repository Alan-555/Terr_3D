using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Engine;
using Terr3D.Utils;

namespace Terr3D;

public sealed class Engine
{
    public const string EnginePrefix = "T3D_";
    private static Engine _instance = null!;
    private readonly EngineInternal _engine;

    
    public static ResourceManager Resources => _instance._engine._resourceManager;

    internal Engine(EngineInternal engine)
    {
        _instance = this;
        _engine = engine;
    }


}

internal sealed class EngineInternal : IDisposable
{
    internal ResourceManager _resourceManager;

    internal EngineInternal()
    {
        _resourceManager = new("res");
        _ = new Engine(this);
        InitClient(InitRest);
    }

    static void InitClient(Action OnResourcesReady)
    {
        var screen = Client.EngineWindow.ConstructScreen(OnResourcesReady);
        screen.Run();
    }

    static void InitRest()
    {
        World.Create();
        Diagnostics.Info("World crated. Spawning scene...");
        Scene scene = new ConcreteScene();
        World.LoadScene(scene);
        EngineWindow.Instance.IsVisible = true;
    }


    public void Dispose()
    {
        _resourceManager.Dispose();
    }
}