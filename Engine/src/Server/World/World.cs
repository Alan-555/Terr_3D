using Terr3D.Server.Components;
using Terr3D.Server.Entities;

namespace Terr3D.Server.Engine;


public static class World
{
    public static Camera? ActiveCamera {get; set;}
    public static Canvas Canvas {get; private init;}
    private static readonly List<Scene> _loadedScenes = new();
    private static readonly Dictionary<Type, SingletonComponent> Singletons = new();

    public static void Register(SingletonComponent instance)
    {
        Type type = instance.GetType();
        if (Singletons.ContainsKey(type))
            throw new InvalidOperationException($"Singleton of type {type.Name} already registered.");
        Singletons[type] = instance;
    }

    public static void LoadScene(Scene scene)
    {
        _loadedScenes.Add(scene);
    }

    public static void UnloadScene(Scene scene)
    {
        _loadedScenes.Remove(scene);
    }

    public static T Get<T>() where T : SingletonComponent =>
        (T)Singletons[typeof(T)]; // or TryGet if you want non-throwing lookups

    public static void EngineLoop(float dt)
    {
        UpdateSingletons(dt);
        UpdateScenes(dt);
    }
    
    private static void UpdateSingletons(float dt)
    {
        foreach (var s in Singletons.Values) s.Update(dt);
    }

    private static void UpdateScenes(float dt)
    {
        foreach(var scene in _loadedScenes)
        {
            scene.UpdateScene(dt);
        }
    }
}