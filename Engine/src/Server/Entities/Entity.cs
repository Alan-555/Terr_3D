using System.Collections.ObjectModel;
using System.Reflection;
using OpenTK.Mathematics;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.Shared;
using Terr3D.Server.World;
using Terr3D.Utils;

namespace Terr3D.Server.Entities;

/// <summary>
/// An entity that can exist in the word. Implements its own custom logic and borrows behaviour from attached components
/// </summary>
public abstract class Entity
{
    public string Name { get; set; }

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
    public List<Component> _components = [];

    /// <summary>
    /// The Parent of this entity
    /// </summary>
    public Entity Parent { get; private set; }

    /// <summary>
    /// List of its children
    /// </summary>
    private readonly List<Entity> _children = [];


    public ReadOnlyCollection<Entity> Children => _children.AsReadOnly();


    public bool IsEnabled => isEnabled;
    bool isEnabled = true;


    private Scene _currentScene;
    public Scene Onstage => _currentScene;


    private static long entId = 0;
    private static long staticEntId = 0;

    private long NextEntId => this.IsStatic ? staticEntId++ : entId++;


    public event Action OnParentChanged;

    public Entity(string name, Entity parent, bool isStatic = false)
    {
        if (name == "")
        {
            name = this.GetType().Name + NextEntId;
        }
        if (name.Contains('$'))
            name = name.Replace("$", Name + NextEntId);
        IsStatic = isStatic;
        Name = name;
        Transform = new(this);
        
        if(this is Worldspawn)
        {
            return;
        }


        if (parent == null)
        {
            Diagnostics.Error($"Orphaned entity {this} could not set its parent!");
            return;
        }

        SetParent(parent);
        CacheWorldspawn();
    }

    public void InstantiateEmptyChild(string name) => InstantiateEmpty(name, this, IsStatic);


    public static EmptyEntity InstantiateEmpty(string name, Entity parent, bool isStatic = false)
    {
        return Instantiate(()=>new EmptyEntity(name, parent, isStatic));
    }

    public static T Instantiate<T>(Func<T> factory) where T : Entity
    {
        T entity = factory();
        entity.InitialiseSubtree();
        return entity;
    }

    private static readonly Dictionary<Type, FieldInfo[]> _dependencyFieldCache = new();

    private void ResolveDependencies(Component component)
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

        foreach (FieldInfo field in fields)
        {
            List<Component> matches = _components
                .Where(c => c != component && field.FieldType.IsInstanceOfType(c))
                .ToList();

            if (matches.Count == 0)
                throw new InvalidOperationException(
                    $"{type.Name}.{field.Name}: no component of type {field.FieldType.Name} found on entity.");

            if (matches.Count > 1)
                throw new InvalidOperationException(
                    $"{type.Name}.{field.Name}: multiple candidates for {field.FieldType.Name} found on entity " +
                    $"({string.Join(", ", matches.Select(m => m.GetType().Name))}). Disambiguate with a subtype.");

            field.SetValue(component, matches[0]);
        }
    }

    private void InitialiseSubtree()
    {
        foreach (Component component in _components)
            component.OnInitialise();

        OnInitialise();

        foreach (Entity child in Children)
            child.InitialiseSubtree();
    }

    public void SetParent(Entity? parent)
    {
        parent ??= Onstage.Worldspawn;
        
        if(Parent != null)
        {
            //we've been disowned :(
            Parent._children.Remove(this);
        }

        Parent = parent;
        Parent._children.Add(this);
        OnParentChanged.Invoke();
    }

    private void CacheWorldspawn()
    {
        var parent = this;
        while (parent.Parent != null)
        {
            parent = parent.Parent;
        }
        _currentScene = ((Worldspawn)parent).Scene;
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
        ResolveDependencies(component);
        return component;
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

    /*void OnTransformUpdate()
    {
        if (IsStatic) return;
        foreach (var component in Components)
            component.Thinker.OnTransformUpdate();
        foreach (var child in _children)
            child.Thinker.OnTransformUpdate();
    }*/

    public void SetEnabled(bool state)
    {
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



    public override string ToString() => $"{Name}[{GetType()}] @ {Transform}";

    public virtual void OnInitialise() { }

    public virtual void OnEnable() { }

    public virtual void OnDisable() { }

    public void Destroy()
    {
        IsDestroyed = true;
        OnDestroyed();
        foreach (var c in Components)
        {
            c.Destroy();
        }
        _components.Clear();
        foreach (var e in _children)
        {
            e.Destroy();
        }
        Parent._children.Remove(this);
    }

    public virtual void OnDestroyed() { }
}