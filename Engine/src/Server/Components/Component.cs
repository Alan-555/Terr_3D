using Terr3D.Server.Shared;

namespace Terr3D.Server.Components;


/// <summary>
/// A base class for all components. Components provide general behaviour that are used across multiple Entities
/// </summary>
public abstract class Component : IThinker
{
    public IThinker Thinker => this;

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
    public Transform Transform => Entity.Transform;

    public bool HasPriorityUpdate = false;

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

//TODO: unify destroy and enable functions into the thinker
    public void Destroy()
    {
        IsDestroyed = true;
        Entity.Onstage.SceneRegistry.UnregisterComponent(this);
        OnDestroyed();
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual void OnInitialise() { }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual void OnEnable() { }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual void OnDisable() { }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual void OnDestroyed() { }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual void OnTransformUpdate() { }

}
