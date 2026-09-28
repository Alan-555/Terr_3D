using Terr3D.Server.Shared;
using Terr3D.Utils;

namespace Terr3D.Server.Components;


/// <summary>
/// A base class for all components. Components provide general behaviour that are used across multiple Entities
/// </summary>
public abstract class Component
{

    /// <summary>
    /// The entity this component is bound to
    /// </summary>
    public Entities.Entity Entity { get; private set; } = null!;

    /// <summary>
    /// Weather this component has been destroyed
    /// </summary>
    public bool IsDestroyed { get; private set; } = false;

    /// <summary>
    /// A shortcut to the entity's transform
    /// </summary>
    public Entities.Transform Transform => Entity.Transform;

    /// <summary>
    /// A shortcut to the entity's Onstage
    /// </summary>
    public Engine.Scene Onstage => Entity.Onstage;


    /// <summary>
    /// Is this component enabled?
    /// </summary>
    public bool IsEnabled => isEnabled;
    bool isEnabled = true;

    private bool _wasInitialised = false;

    /// <summary>
    /// Changes the components enabled state
    /// </summary>
    /// <param name="state">New state</param>
    public void SetEnabled(bool state)
    {
        if(state == isEnabled) return;

        isEnabled = state;
        if (state)
            OnEnable();
        else
            OnDisable();
    }

    /// <summary>
    /// Bind this component to an entity. This should only be called once!
    /// </summary>
    /// <param name="entity">Our entity</param>
    internal void Bind(Entities.Entity entity)
    {
        Entity = entity;
    }

    /// <summary>
    /// Destroys the component by removing it from the entity
    /// </summary>
    public void Destroy()
    {
        if (IsDestroyed)
        {
            Diagnostics.Warn($"Destroying an already destroyed component {this}");
            return;
        }
        IsDestroyed = true;
        Entity.Onstage.SceneRegistry.UnregisterComponent(this);
        Entity.RemoveComponent(this);
        OnDestroyed();
    }

    internal void Initialise()
    {
        if (_wasInitialised)
        {
            Diagnostics.Warn($"Component {this} was already initialised!");
            return;
        }
        _wasInitialised = true;
        OnInitialise();
    }

    /// <summary>
    /// Called once the component sits on an Entity and all dependencies are resolved
    /// </summary>
    protected virtual void OnInitialise() { }

    /// <summary>
    /// Called when the component has been enabled (and had been previously disabled)
    /// </summary>
    protected virtual void OnEnable() { }

    /// <summary>
    /// Called when the component has been disabled (and hada been previously enabled)
    /// </summary>
    protected virtual void OnDisable() { }

    /// <summary>
    /// Called once the component is destroyed
    /// </summary>
    protected virtual void OnDestroyed() { }

    internal void TransformUpdated() => OnTransformUpdate();

    /// <summary>
    /// Called when the entity this component is attached to moves in world-space
    /// </summary>
    protected virtual void OnTransformUpdate() { }

    public override string ToString() => $"{GetType().Name} on {Entity}";


}

[AttributeUsage(AttributeTargets.Field)]
public sealed class DependencyAttribute : Attribute { }