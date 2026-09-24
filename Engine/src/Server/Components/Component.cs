using Terr3D.Server.Shared;

namespace Terr3D.Server.Components;


/// <summary>
/// A base class for all components. Components provide general behaviour that are used across multiple Entities
/// </summary>
public abstract class Component
{

    /// <summary>
    /// The entity this component is bound to
    /// </summary>
    public Entities.Entity Entity { get; private set; }

    /// <summary>
    /// Weather this component has been destroyed
    /// </summary>
    public bool IsDestroyed { get; private set; } = false;

    /// <summary>
    /// A shortcut to the entity's transform
    /// </summary>
    public Entities.Transform Transform => Entity.Transform;

    public Engine.Scene Onstage => Entity.Onstage;


    /// <summary>
    /// Is this component enabled?
    /// </summary>
    public bool IsEnabled => isEnabled;
    bool isEnabled = true;

    /// <summary>
    /// Changes the components enabled state
    /// </summary>
    /// <param name="state">New state</param>
    public void SetEnabled(bool state)
    {
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
    public void Bind(Entities.Entity entity)
    {
        Entity = entity;
    }

    public void Destroy()
    {
        IsDestroyed = true;
        Entity.Onstage.SceneRegistry.UnregisterComponent(this);
        OnDestroyed();
    }

    /// <summary>
    /// Called once the component sits on an Entity and all dependencies are resolved
    /// </summary>
    public virtual void OnInitialise() { }

    /// <summary>
    /// Called when the component has been enabled (and had been previously disabled)
    /// </summary>
    public virtual void OnEnable() { }

    /// <summary>
    /// Called when the component has been disabled
    /// </summary>
    public virtual void OnDisable() { }

    /// <summary>
    /// Called once the component is destroyed
    /// </summary>
    public virtual void OnDestroyed() { }

    /// <summary>
    /// Called when the entity this component is attached to moves in world-space
    /// </summary>
    public virtual void OnTransformUpdate() { }

}

[AttributeUsage(AttributeTargets.Field)]
public sealed class DependencyAttribute : Attribute { }