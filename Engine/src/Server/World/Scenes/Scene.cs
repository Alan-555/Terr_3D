using Terr3D.Client.Resources;
using Terr3D.Server.Entities;
using Terr3D.Utils;

namespace Terr3D.Server.Core;

public abstract class Scene
{

    private static int nextSceneId = 0;
    public int Id { get; private init; } = nextSceneId++;
    public Worldspawn Worldspawn { get; private init; }
    public SceneRegistry SceneRegistry { get; private init; } = new();

    internal QuadTreeNode SceneNode { get; private init; }

    internal bool spawningStatic = true;

    private readonly ResourceManager manager;
    private readonly ResourceScope scope;
    private readonly List<string> runtimePaths = new();

    internal Scene()
    {
        manager = Engine.Resources;
        scope = new(manager);

        Diagnostics.Info("Initialising scene...");
        Worldspawn = new(this);
        SpawnStaticEntities();
        spawningStatic = false;
        Diagnostics.Info("Done creating static entities. Partitioning...");
        SceneNode = QuadTreeBuilder.Construct(this);
        Diagnostics.Info("Static entities partitioned. Spawning dynamic entities...");
        SpawnDynamicEntities();
    }

    public abstract void SpawnStaticEntities();
    public abstract void SpawnDynamicEntities();


    /// <summary>
    /// Updates all enabled entities and their components
    /// </summary>
    /// <param name="dt">The deltatime</param>
    public void UpdateScene(float dt)
    {
        //iterate all updatable objects
        foreach (var updatable in SceneRegistry.BehaviourComponents)
        {
            //check if the subject is enabled
            if (updatable.IsDestroyed || !updatable.IsEnabled) continue;

            updatable.Update(dt);
        }
    }



    public void DestroyScene()
    {
        /*Entity[] tempE = new Entity[SceneRegistry.Entities.Count];
         SceneRegistry.Entities.CopyTo(tempE);
         foreach (var ent in tempE)
         {
             ent.Destroy();
         }*/

    }



    public T? FindEntityOfType<T>() where T : Entity
    {
        foreach (var child in Worldspawn)
        {
            if (child is T c)
            {
                return c;
            }
        }

        return null;
    }



    #region Resources

    /// <summary>
    /// Loads a resource which is guaranteed to live until this scene unloads, then it may be collected
    /// </summary>
    /// <typeparam name="T">A concrete type of a Resource</typeparam>
    /// <param name="path">Path to that resource</param>
    /// <returns>The resource instance</returns>
    public T Load<T>(string path) where T : Resource => scope.Use(manager.Get<T>(path));

    /// <summary>
    /// Loads a resource that you have a reference to. They resource may not be loaded
    /// </summary>
    /// <typeparam name="T">A concrete type of a Resource</typeparam>
    /// <param name="r">The resource ref</param>
    /// <returns>The loaded resource</returns>
    public T Load<T>(ResourceRef<T> r) where T : Resource => scope.Use(r);

    /// <summary>
    /// Creates a dynamic Resource (a resource that does not exist on disk)
    /// </summary>
    /// <typeparam name="T">A concrete type of a Resource</typeparam>
    /// <param name="name">The name of this resource</param>
    /// <param name="factory">The factory that constructs this resource</param>
    /// <returns>The loaded resource</returns>
    public T Create<T>(string name, Func<T> factory, string setPath = "") where T : Resource
    {
        setPath = setPath.Trim();
        if(setPath == "")
        {
            setPath = $"scene{Id}";
        }
        else if(setPath.EndsWith('/'))
        {
            setPath = setPath[0..(setPath.Length - 1)];
        }
        var path = $"{Resource.DynamicRes}{setPath}/{name}";
        if (!manager.TryGet<T>(path, out var r))
        {
            r = manager.Register(path, factory);
            runtimePaths.Add(path);
        }
        return scope.Use(r);
    }

    internal void Unload()
    {
        scope.ReleaseAll();
        foreach (var path in runtimePaths)
            manager.Unregister(path);
        runtimePaths.Clear();
    }

    #endregion
}


public class EmptyScene() : Scene()
{
    public override void SpawnDynamicEntities() { }
    public override void SpawnStaticEntities() { }
}