using System.Collections.ObjectModel;
using System.Reflection;
using OpenTK.Mathematics;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.Shared;
using Terr3D.Server.Core;
using Terr3D.Utils;
using System.Collections;

namespace Terr3D.Server.Entities;

/// <summary>
/// An entity that can exist in the word. Implements its own custom logic and borrows behaviour from attached components
/// </summary>
public abstract class Entity : IEnumerable<Entity>
{
    /// <summary>
    /// The name of this entity in the hierarchy
    /// </summary>
    public virtual string Name { get; set; }

    /// <summary>
    /// The transformation of this entity
    /// </summary>
    public Transform Transform { get; private init; }

    /// <summary>
    /// Whether this entity has been destroyed
    /// </summary>
    public bool IsDestroyed { get; private set; }

    /// <summary>
    /// Whether this entity is static
    /// </summary>
    public bool IsStatic { get; private set; }


    /// <summary>
    /// List of all attached components
    /// </summary>
    public ReadOnlyCollection<Component> Components => _components.AsReadOnly();
    private List<Component> _components = [];


    /// <summary>
    /// The Parent of this entity
    /// </summary>
    public Entity Parent { get; private set; } = null!;


    /// <summary>
    /// List of its children
    /// </summary>
    public ReadOnlyCollection<Entity> Children => _children.AsReadOnly();
    private readonly List<Entity> _children = [];

    /// <summary>
    /// Whether this entity is enabled or not
    /// </summary>
    public bool IsEnabled => isEnabled;
    private bool isEnabled = true;

    /// <summary>
    /// Whether this entity has been initialised
    /// </summary>
    private bool _wasInitialised = false;

    /// <summary>
    /// The current scene this entity is bound to
    /// </summary>
    public Scene Onstage => _currentScene;

    /// <summary>
    /// The cached current scene, which never changes
    /// </summary>
    private Scene _currentScene = null!;


    private static long entId = 0;
    private static long staticEntId = 0;

    /// <summary>
    /// Returns the next available entity ID
    /// </summary>
    private long NextEntId => IsStatic ? staticEntId++ : entId++;

    /// <summary>
    /// An event that is invoked when the parent changes
    /// </summary>
    internal event Action? OnParentChanged;

    public Entity(string name, Entity parent)
    {
        if (name == "")
        {
            name = this.GetType().Name + NextEntId;
        }
        if (name.Contains('$'))
            name = name.Replace("$", Name + NextEntId);
        Name = name;
        Transform = new(this);

        if (this is Worldspawn)
        {
            return;
        }


        if (parent == null)
        {
            //TODO: decide what is just an error and what should throw an exception
            Diagnostics.Error($"Orphaned entity {this} could not set its parent!");
            return;
        }

        SetParent(parent);
        CacheWorldspawn();
        IsStatic = Onstage.spawningStatic;
    }

    #region Utils and props

    private void CacheWorldspawn()
    {
        var parent = this;
        while (parent.Parent != null)
        {
            parent = parent.Parent;
        }
        _currentScene = ((Worldspawn)parent).Scene;
    }

    public override string ToString() => $"{FullName}[{GetType().Name}] @ {Transform}";

    public virtual string FullName
    {
        get
        {
            if (Parent != null) return $"{Parent.FullName}.{Name}";
            else return Name;

        }
    }


    public IEnumerator<Entity> GetEnumerator()
    {
        return _children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _children.GetEnumerator();
    }

    public void SetParent(Entity? parent)
    {
        parent ??= Onstage.Worldspawn;

        if (IsInSubtreeOf(parent, this))
        {
            throw new InvalidOperationException($"Cannot set {parent} as the parent of {this}. The target parent is in the subtree of the target entity!");
        }

        if (Parent != null)
        {
            //we've been disowned :(
            Parent._children.Remove(this);
        }

        Parent = parent;
        Parent._children.Add(this);
        OnParentChanged?.Invoke();
    }

    #endregion

    #region  Components

    private static readonly Dictionary<Type, FieldInfo[]> _dependencyFieldCache = new();

    private static void CollectComponents(Entity entity, List<Component> into)
    {
        into.AddRange(entity.Components);
        foreach (Entity child in entity.Children)
            CollectComponents(child, into);
    }

    internal static void ResolveDependencies(Entity subtreeRoot, Component component)
    {
        Type type = component.GetType();

        if (!_dependencyFieldCache.TryGetValue(type, out FieldInfo[] fields))
        {
            fields = type
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => f.GetCustomAttribute<DependencyAttribute>() != null)
                .ToArray();
            _dependencyFieldCache[type] = fields;
        }

        if (fields.Length == 0) return;

        List<Component> pool = new();
        CollectComponents(subtreeRoot, pool);

        foreach (FieldInfo field in fields)
        {
            List<Component> matches = pool
                .Where(c => c != component && field.FieldType.IsInstanceOfType(c))
                .ToList();

            if (matches.Count == 0)
                throw new InvalidOperationException(
                    $"{type.Name}.{field.Name}: no component of type {field.FieldType.Name} found in subtree.");

            if (matches.Count > 1)
                throw new InvalidOperationException(
                    $"{type.Name}.{field.Name}: multiple candidates for {field.FieldType.Name} " +
                    $"({string.Join(", ", matches.Select(m => m.GetType().Name))}). Disambiguate with a subtype.");

            field.SetValue(component, matches[0]);
        }
    }



    public Entity WithComponent<T>() where T : Component, new()
    {
        AddComponent<T>();
        return this;
    }

    public Entity WithComponents(params Component[] components)
    {
        foreach (var c in components)
            AddComponent(c);
        return this;
    }

    public Entity WithComponent(Component component)
    {
        AddComponent(component);
        return this;
    }

    public E_T WithComponent<E_T, T>() where T : Component, new() where E_T : Entity
    {
        AddComponent<T>();
        return (E_T)this;
    }





    /// <summary>
    /// Attaches a new component to this entity
    /// </summary>
    /// <typeparam name="T">The classname of the component to instantiate and attach</typeparam>
    /// <returns>The instantiated and attached entity</returns>
    public T AddComponent<T>() where T : Component, new()
    {
        return AddComponent(new T());
    }

    public T AddComponent<T>(T component) where T : Component
    {
        component.Bind(this);
        _components.Add(component);
        Onstage.SceneRegistry.RegisterComponent(component);
        //if we've already been initialised, we initialise the new component right away
        if (_wasInitialised)
            component.Initialise();
        return component;
    }

    internal void RemoveComponent(Component component)
    {
        _components.Remove(component);
    }


    public T AddComponent_<T>(T component) where T : Component
    {
        return AddComponent(component);
    }

    /// <summary>
    /// Returns the first component of type T attached to this entity
    /// </summary>
    public T? GetComponent<T>() where T : Component
    {
        return Components.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Tries to get the component of type T
    /// </summary>
    public bool TryGetComponent<T>(out T component) where T : Component
    {
        component = GetComponent<T>()!;
        return component != null;
    }
    #endregion
    #region  Events

    internal void TransformUpdated() => OnTransformUpdated();


    private void OnTransformUpdated()
    {
        if (IsStatic) return;
        foreach (var component in Components)
            component.TransformUpdated();
        foreach (var child in _children)
            child.OnTransformUpdated();
    }

    public void SetEnabled(bool state)
    {
        if (state == isEnabled) return;

        isEnabled = state;
        if (state)
            OnEnable();
        else
            OnDisable();
        foreach (var component in Components)
        {
            component.SetEnabled(state);
        }
        foreach (var child in _children)
        {
            child.SetEnabled(isEnabled); //TODO: the children should remember their enabled state and always submit to their parent
        }
    }




    internal void Initialise()
    {
        if (_wasInitialised) return;
        OnInitialise();
    }

    protected virtual void OnInitialise() { }

    protected virtual void OnEnable() { }

    protected virtual void OnDisable() { }

    public void Destroy()
    {
        IsDestroyed = true;
        OnDestroyed();
        Component[] compCache = new Component[_components.Count];
        _components.CopyTo(compCache);
        foreach (var c in compCache)
        {
            c.Destroy();
        }
        Entity[] cache = new Entity[_children.Count];
        _children.CopyTo(cache);
        foreach (var e in cache)
        {
            e.Destroy();
        }
        Parent?._children.Remove(this);
        Parent = null!;
    }

    protected virtual void OnDestroyed() { }

    #region Resources
    /// <summary>
    /// Loads a resource which is guaranteed to live until this scene unloads, then it may be collected
    /// </summary>
    /// <typeparam name="T">A concrete type of a Resource</typeparam>
    /// <param name="path">Path to that resource</param>
    /// <returns>The resource instance</returns>
    public T Load<T>(string path) where T : Resource => Onstage.Load<T>(path);

    /// <summary>
    /// Loads a resource that you have a reference to. They resource may not be loaded
    /// </summary>
    /// <typeparam name="T">A concrete type of a Resource</typeparam>
    /// <param name="r">The resource ref</param>
    /// <returns>The loaded resource</returns>
    public T Load<T>(ResourceRef<T> r) where T : Resource => Onstage.Load(r);

    /// <summary>
    /// Creates a dynamic Resource (a resource that does not exist on disk)
    /// </summary>
    /// <typeparam name="T">A concrete type of a Resource</typeparam>
    /// <param name="name">The name of this resource</param>
    /// <param name="factory">The factory that constructs this resource</param>
    /// <returns>The loaded resource</returns>
    public T Create<T>(string name, Func<T> factory) where T : Resource => Onstage.Create(name, factory);

    #endregion

    #endregion
    #region Entity API

    /// <summary>
    /// Instantiates an empty child of this entity (static flag is inherited)
    /// </summary>
    /// <param name="name">The name of the new child</param>
    public void InstantiateEmptyChild(string name) => InstantiateEmpty(name, this);

    /// <summary>
    /// Instantiates new and empty child
    /// </summary>
    /// <param name="name">The name of the empty child</param>
    /// <param name="parent">The parent of the child</param>
    /// <param name="isStatic">If the entity is static or not</param>
    /// <returns>The EmptyEntity that was instantiated</returns>
    public static EmptyEntity InstantiateEmpty(string name, Entity parent)
    {
        return Instantiate(() => new EmptyEntity(name, parent));
    }

    /// <summary>
    /// Instantiates new and empty child with the given components
    /// </summary>
    /// <param name="name">The name of the empty child</param>
    /// <param name="parent">The parent of the child</param>
    /// <param name="isStatic">If the entity is static or not</param>
    /// <param name="components">The component instances to bind to the new entity</param>
    /// <returns>The EmptyEntity that was instantiated</returns>
    public static EmptyEntity InstantiateEmptyWith(string name, Entity parent, params Component[] components)
    {
        return Instantiate(() => (EmptyEntity)new EmptyEntity(name, parent).WithComponents(components));

    }

    /// <summary>
    /// Instantiates a new entity using the factory that was passed
    /// </summary>
    /// <typeparam name="T">The type of the Entity</typeparam>
    /// <param name="factory">The factory to construct the entity with</param>
    /// <returns>The instantiated entity</returns>
    public static T Instantiate<T>(Func<T> factory) where T : Entity //TODO: remove factory pattern
    {
        T entity = factory();
        EntityLifecycle.InitialiseSubtree(entity);
        return entity;
    }

    //TODO: decide name path name
    public static Entity? FindEntityByName(string name)
    {
        foreach (var scene in World.LoadedScenes)
        {
            var ent = FindEntityByName(name, scene.Worldspawn);
            if (ent != null) return ent;
        }
        return null;
    }

    private static Entity? FindEntityByName(string name, Entity root)
    {
        if (root.Name == name) return root;
        foreach (var child in root.Children)
        {
            var ent = FindEntityByName(name, child);
            if (ent != null) return ent;
        }
        return null;
    }

    public static bool IsInSubtreeOf(Entity target, Entity root)
    {
        var next = target;
        while (next != null)
        {
            if (next == root) return true;
            next = next.Parent;
        }
        return false;
    }

    public static IEnumerable<Entity> GetAllEntities()
    {
        foreach (var scene in World.LoadedScenes)
        {
            foreach (var entity in CollectEntitiesIterative(scene.Worldspawn))
            {
                yield return entity;
            }
        }
    }

    private static IEnumerable<Entity> CollectEntitiesIterative(Entity root)
    {
        var stack = new Stack<Entity>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            foreach (var child in current.Reverse())
            {
                stack.Push(child);
            }
        }
    }
    #endregion
}

public static class EntityLifecycle
{
    public static void InitialiseSubtree(Entity root)
    {
        ResolveSubtree(root, root);
        InitialiseAll(root);
    }

    private static void ResolveSubtree(Entity root, Entity node)
    {
        foreach (Component component in node.Components)
            Entity.ResolveDependencies(root, component);

        foreach (Entity child in node.Children)
            ResolveSubtree(root, child);
    }

    private static void InitialiseAll(Entity node)
    {
        foreach (Component component in node.Components)
            component.Initialise();

        node.Initialise();

        foreach (Entity child in node.Children)
            InitialiseAll(child);
    }
}