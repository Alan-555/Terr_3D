using System.Collections.ObjectModel;
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
public abstract class Entity : IThinker, IUpdates
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
    public List<Component> components = [];

    /// <summary>
    /// The Parent of this entity
    /// </summary>
    public Entity Parent { get; private set; }

    /// <summary>
    /// List of its children
    /// </summary>
    private readonly List<Entity> _children = [];


    public ReadOnlyCollection<Entity> Children => _children.AsReadOnly();

    public IThinker Thinker => this;

    public bool IsEnabled => isEnabled;
    bool isEnabled = true;

    Worldspawn _worldSpawn;

    public Worldspawn Worldspawn => _worldSpawn;


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

        if (Parent == null)
        {
            Diagnostics.Error($"Orphaned entity {this} could not set its parent!");
            return;
        }

        SetParent(parent);
        CacheWorldspawn();
    }

    public void SetParent(Entity? parent)
    {
        parent ??= _worldSpawn;
        
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
        _worldSpawn = (Worldspawn)parent;
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
        components.Add(component);
        Onstage.SceneRegistry.RegisterComponent(component);
        component.OnInitialise();
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
        return components.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Tries to get the component of type T
    /// </summary>
    public bool TryGetComponent<T>(out T component) where T : Component
    {
        component = GetComponent<T>()!;
        return component != null;
    }

    void IThinker.OnTransformUpdate()
    {
        if (IsStatic) return;
        foreach (var component in components)
            component.Thinker.OnTransformUpdate();
        foreach (var child in children)
            child.Entity.Thinker.OnTransformUpdate();
    }

    public void SetEnabled(bool state)
    {
        isEnabled = state;
        if (state)
            OnEnable();
        else
            OnDisable();
        foreach (var component in components)
        {
            component.SetEnabled(state);
        }
        foreach (var child in children)
        {
            child.Entity.SetEnabled(isEnabled);
        }
    }



    public override string ToString() => $"{Name}[{GetType()}] @ {Transform}";

    public virtual void OnInitialise() { }

    public virtual void OnUpdate(float dt) { }

    public virtual void OnRender() { }

    public virtual void OnEnable() { }

    public virtual void OnDisable() { }

    public void Destroy()
    {
        IsDestroyed = true;
        Onstage.SceneRegistry.UnregisterEntity(this);
        OnDestroyed();
        foreach (var c in components)
        {
            c.Destroy();
        }
        components.Clear();
        foreach (var e in children)
        {
            e.Entity.Destroy();
        }
        if (Transform._parent != null)
        {
            if (!Transform._parent.Entity.IsDestroyed)
                Transform._parent.Entity.children.Remove(Transform);
        }
    }
    public virtual void OnDestroyed() { }
}