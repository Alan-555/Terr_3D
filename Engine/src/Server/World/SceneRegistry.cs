using Terr3D.Server.Components;
using Terr3D.Server.Entities;
using Terr3D.Server.Shared;
using Terr3D.Utils;

namespace Terr3D.Server.World;

/// <summary>
/// Used to register entities and other scene-wide accessible things. 
/// Acts as a central database for all components and renderable objects.
/// </summary>
public class SceneRegistry
{
    /// <summary>
    /// All registered entities
    /// </summary>
    public readonly List<Entity> Entities = [];

    /// <summary>
    /// All components and entities that need to be updated every frame
    /// </summary>
    public readonly List<IUpdates> UpdatableObjects = [];

    private readonly List<IUpdates> _pendingRegistrations = [];
    private readonly List<IUpdates> _pendingUnregistrations = [];
    private bool _isUpdating = false;

    public void BeginUpdate() => _isUpdating = true;
    public void EndUpdate()
    {
        _isUpdating = false;
        foreach (var item in _pendingRegistrations)
        {
            if (!UpdatableObjects.Contains(item))
                UpdatableObjects.Add(item);
        }
        foreach (var item in _pendingUnregistrations)
        {
            UpdatableObjects.Remove(item);
        }
        _pendingRegistrations.Clear();
        _pendingUnregistrations.Clear();
    }

    /// <summary>
    /// All registered components indexed by their type for fast querying
    /// </summary>
    private readonly Dictionary<Type, List<Component>> _componentsByType = [];

    /// <summary>
    /// Static renderers managed by Quadtree
    /// </summary>
    public readonly List<Renderer> StaticRenderers = [];

    /// <summary>
    /// Dynamic renderers that move and need frustum culling every frame
    /// </summary>
    public readonly List<Renderer> DynamicRenderers = [];

    /// <summary>
    /// UI renderers that are drawn in a separate overlay pass
    /// </summary>
    public readonly List<Renderer> UIRenderers = [];

    public readonly List<ParticleSystem> ParticleSystems = [];


    public void RegisterEntity(Entity entity)
    {
        if (Entities.Contains(entity))
        {
            Diagnostics.Warn($"Registering existing entity {entity}");
            return;
        }

        Entities.Add(entity);

        if (entity is IUpdates updatable)
        {
            if (_isUpdating) _pendingRegistrations.Add(updatable);
            else UpdatableObjects.Add(updatable);
        }

        // Components added before registration need to be registered too
        foreach (var component in entity.components)
        {
            RegisterComponent(component);
        }
    }

    public void UnregisterEntity(Entity entity)
    {
        Entities.Remove(entity);
        if (entity is IUpdates updatable)
        {
            if (_isUpdating) _pendingUnregistrations.Add(updatable);
            else UpdatableObjects.Remove(updatable);
        }

        foreach (var component in entity.components)
        {
            UnregisterComponent(component);
        }
    }

    public void RegisterComponent(Component component)
    {
        // Add to type-based dictionary
        var type = component.GetType();
        if (!_componentsByType.TryGetValue(type, out List<Component>? value))
        {
            value = [];
            _componentsByType[type] = value;
        }

        value.Add(component);

        // Add to updatable list if it implements IUpdates
        if (component is IUpdates updatable)
        {
            if (_isUpdating) _pendingRegistrations.Add(updatable);
            else UpdatableObjects.Add(updatable);
        }

        if (component is ParticleSystem particleSystem)
        {
            ParticleSystems.Add(particleSystem);
        }

        // Handle Renderers
        if (component is Renderer renderer)
        {
            switch (renderer.rendererClass)
            {
                case RendererClass.RENDERER_DYNAMIC:
                    DynamicRenderers.Add(renderer);
                    break;
                case RendererClass.RENDERER_STATIC:
                    StaticRenderers.Add(renderer);
                    break;
                case RendererClass.RENDERER_UI:
                    StaticRenderers.Add(renderer);
                    break;
                case RendererClass.RENDERER_PERSISTENT:
                    DynamicRenderers.Add(renderer);
                    break;
            }
        }
    }

    public void UnregisterComponent(Component component)
    {
        var type = component.GetType();
        if (_componentsByType.TryGetValue(type, out var list))
        {
            list.Remove(component);
        }

        if (component is IUpdates updatable)
        {
            if (_isUpdating) _pendingUnregistrations.Add(updatable);
            else UpdatableObjects.Remove(updatable);
        }

        if (component is ParticleSystem particleSystem)
        {
            ParticleSystems.Remove(particleSystem);
        }

        if (component is Renderer renderer)
        {
            StaticRenderers.Remove(renderer);
            DynamicRenderers.Remove(renderer);
            UIRenderers.Remove(renderer);
        }
    }

    /// <summary>
    /// Returns all components that are assignable to the specified type T (supports abstract classes and interfaces)
    /// </summary>
    public IEnumerable<T> GetComponents<T>() where T : class
    {
        return _componentsByType
            .Where(entry => typeof(T).IsAssignableFrom(entry.Key))
            .SelectMany(entry => entry.Value)
            .Cast<T>();
    }
}