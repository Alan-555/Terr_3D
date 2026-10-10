using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Client.Resources.Importers;
using Terr3D.Server.Core;
using Terr3D.Utils;

namespace Terr3D;

public sealed class Engine
{
    private static Engine _instance = null!;
    private readonly EngineInternal _engine;

    
    internal static ResourceManager Resources => _instance._engine._resourceManager;

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
        RegisterBuiltinImporters();
        _ = new Engine(this);
        InitClient(InitRest);
    }

    void RegisterBuiltinImporters()
    {
        _resourceManager.RegisterImporter(new MeshImporter(), ".obj");
        _resourceManager.RegisterImporter(new TextureImporter(), ".png", ".bmp");
        _resourceManager.RegisterImporter(new ShaderImporter(), ".glsl");
    }

    void InitClient(Action onGpuContextReady)
    {
        var screen = Client.EngineWindow.ConstructScreen(onGpuContextReady);
        screen.Run();
    }

    void InitRest()
    {
        _resourceManager.ScanDirectory();
        World.Create();
        Diagnostics.Info("World crated. Spawning scene...");
        Scene scene = new ConcreteScene();
        World.LoadScene(scene);
        EngineWindow.Instance.IsVisible = true;
    }


    public void Dispose()
    {
        World._instance.Dispose();
        _resourceManager.Dispose();
    }
}