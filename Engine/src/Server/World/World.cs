using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Terr3D.Client;
using Terr3D.Server.Components;
using Terr3D.Server.Entities;
using Terr3D.Utils;

namespace Terr3D.Server.Engine;


public class World : IDisposable
{
    private static World _instance = null!;
    public static Camera? ActiveCamera
    {
        get => _instance._activeCamera;
        set
        {
            _instance._activeCamera = value;
            _instance._activeCamera?.UpdateAspect(EngineWindow.Instance.ClientSize.X, EngineWindow.Instance.ClientSize.Y);
        }
    }
    private Camera? _activeCamera;
    private readonly List<Scene> _loadedScenes = [];

    public static ReadOnlyCollection<Scene> LoadedScenes => _instance._loadedScenes.AsReadOnly();

    private readonly Scene _persistentScene = new EmptyScene();
    private readonly Dictionary<Type, SingletonComponent> Singletons = [];

    private readonly SpacePartitioner _partitioner;
    public static SpacePartitioner Queries => _instance._partitioner;

    private readonly GroundService _groundService = new();
    public static GroundService Ground => _instance._groundService;

    private readonly WorldDrawer _worldDrawer;

    private World()
    {
        if (_instance != null) throw new InvalidOperationException($"Creating another instance of {nameof(World)}");
        _instance = this;

        _partitioner = SpacePartitioner.CreateRootTree();
        _worldDrawer = new();

        _ = new DeveloperFeatures();
        LoadScene(_persistentScene);
    }

    internal static void Create() => _ = new World();
    internal static void DestroyWorld() => _instance.Dispose();


    public static void RegisterSingleton(SingletonComponent singleton, Action destroyed, Component[] dependencies)
    {
        Type type = singleton.GetType();
        if (_instance.Singletons.ContainsKey(type))
            throw new InvalidOperationException($"Singleton of type {type.Name} already registered.");
        _instance.Singletons[type] = singleton;
        Entity.InstantiateEmptyWith(type.Name, _instance._persistentScene.Worldspawn, [singleton, .. dependencies]);

        destroyed += () => _instance.Singletons.Remove(type);
    }

    public static T GetSingleton<T>() where T : SingletonComponent //TODO: add support for caching singletons? And remove the ability to destroy them?
    {
        return (T)_instance.Singletons[typeof(T)];
    }

    public static T? GetSingletonOrNull<T>() where T : SingletonComponent
    {
        SingletonComponent? val = null;
        _instance?.Singletons.TryGetValue(typeof(T), out val);
        return (T?)val;
    }

    public static T? TryGetSingleton<T>() where T : SingletonComponent
    {
        return (T?)_instance.Singletons.GetValueOrDefault(typeof(T));
    }

    public static void LoadScene(Scene scene)
    {
        _instance._partitioner.AddTree(scene.SceneNode);
        _instance._loadedScenes.Add(scene);
        Diagnostics.Info($"Loaded scene {scene.Worldspawn.Name}");
    }

    public static void UnloadScene(Scene scene)
    {
        _instance._loadedScenes.Remove(scene);
    }

    public static void EngineLoop(float dt)
    {
        UpdateScenes(dt);
    }

    private static void UpdateScenes(float dt)
    {
        foreach (var scene in _instance._loadedScenes)
        {
            scene.UpdateScene(dt);
        }
    }

    public static void DrawWorld()
    {
        _instance._worldDrawer.RenderWorld();
    }

    public static IEnumerable<Renderer> GetAllRenderers(RendererClass rClass)
    {
        foreach (var scene in _instance._loadedScenes)
        {
            var renderers = rClass switch
            {
                RendererClass.RENDERER_DYNAMIC => scene.SceneRegistry.DynamicRenderers,
                RendererClass.RENDERER_STATIC => scene.SceneRegistry.StaticRenderers,
                RendererClass.RENDERER_UI => scene.SceneRegistry.UIRenderers,
                RendererClass.RENDERER_PERSISTENT => throw new NotImplementedException(),
                RendererClass.RENDER_IGNORE => Enumerable.Empty<Renderer>(),
                _ => Enumerable.Empty<Renderer>()
            };

            foreach (var renderer in renderers)
            {
                yield return renderer;
            }
        }
    }

    public static IEnumerable<ParticleSystem> GetAllParticleSystems()
    {
        foreach (var scene in _instance._loadedScenes)
        {
            var particles = scene.SceneRegistry.ParticleSystems;

            foreach (var particle in particles)
            {
                yield return particle;
            }
        }
    }

    public void Dispose()
    {
        _worldDrawer.Dispose();
        GC.SuppressFinalize(this);
    }
}