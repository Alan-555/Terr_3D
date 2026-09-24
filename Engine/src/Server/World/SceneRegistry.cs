using System.Collections.ObjectModel;
using Terr3D.Server.Components;
using Terr3D.Server.Entities;
using Terr3D.Server.Shared;
using Terr3D.Utils;

namespace Terr3D.Server.Engine;

/// <summary>
/// Used to register entities and other scene-wide accessible things. 
/// Acts as a central database for all components and renderable objects.
/// </summary>
public class SceneRegistry
{
    /// <summary>
    /// All components that need to be updated every frame
    /// </summary>
    private readonly List<BehaviourComponent> _updatableComponents = [];

    public ReadOnlyCollection<BehaviourComponent> BehaviourComponents => _updatableComponents.AsReadOnly();

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
        if (component is BehaviourComponent updatable)
        {
            _updatableComponents.Add(updatable);
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

        if (component is BehaviourComponent updatable)
        {
            _updatableComponents.Remove(updatable);
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