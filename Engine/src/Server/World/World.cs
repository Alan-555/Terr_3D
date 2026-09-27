using System.Collections.ObjectModel;
using Terr3D.Client;
using Terr3D.Server.Components;
using Terr3D.Server.Entities;

namespace Terr3D.Server.Engine;


public class World
{
    public static World Instance { get; private set; }

    public Camera? ActiveCamera { get; set; }
    private readonly List<Scene> _loadedScenes = [];

    public static ReadOnlyCollection<Scene> LoadedScenes => Instance._loadedScenes.AsReadOnly();

    private readonly Scene _persistentScene = new EmptyScene();
    private readonly Dictionary<Type, SingletonComponent> Singletons = [];

    private readonly SpacePartitioner _partitioner;
    public static SpacePartitioner Queries => Instance._partitioner;

    private readonly WorldDrawer _worldDrawer;

    public World()
    {
        if (Instance != null) throw new InvalidOperationException($"Creating another instance of {nameof(World)}");
        Instance = this;

        _partitioner = SpacePartitioner.CreateRootTree();
        _worldDrawer = new();

        _ = new ConsoleWindow();
        LoadScene(_persistentScene);
    }

    public static void Create() => _ = new World();


    public static void RegisterSingleton(SingletonComponent singleton, Action destroyed, Component[] dependencies)
    {
        Type type = singleton.GetType();
        if (Instance.Singletons.ContainsKey(type))
            throw new InvalidOperationException($"Singleton of type {type.Name} already registered.");
        Instance.Singletons[type] = singleton;
        Entity.InstantiateEmptyWith(type.Name, Instance._persistentScene.Worldspawn, false, [singleton, .. dependencies]);

        destroyed += () => Instance.Singletons.Remove(type);
    }

    public static T GetSingleton<T>() where T : SingletonComponent //TODO: add support for caching singletons? And remove the ability to destroy them?
    {
        return (T)Instance.Singletons[typeof(T)];
    }

    public static T? TryGetSingleton<T>() where T : SingletonComponent
    {
        return (T?)Instance.Singletons.GetValueOrDefault(typeof(T));
    }

    public static void LoadScene(Scene scene)
    {
        Instance._partitioner.AddTree(scene.SceneNode);
        Instance._loadedScenes.Add(scene);
    }

    public static void UnloadScene(Scene scene)
    {
        Instance._loadedScenes.Remove(scene);
    }

    public static void EngineLoop(float dt)
    {
        UpdateScenes(dt);
    }

    private static void UpdateScenes(float dt)
    {
        foreach (var scene in Instance._loadedScenes)
        {
            scene.UpdateScene(dt);
        }
    }

    public static void DrawWorld()
    {
        Instance._worldDrawer.RenderWorld();
    }

    public static IEnumerable<Renderer> GetAllRenderers(RendererClass rClass)
    {
        foreach (var scene in Instance._loadedScenes)
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
        foreach (var scene in Instance._loadedScenes)
        {
            var particles = scene.SceneRegistry.ParticleSystems;

            foreach (var particle in particles)
            {
                yield return particle;
            }
        }
    }
}