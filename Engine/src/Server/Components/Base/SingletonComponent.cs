using System.Collections.ObjectModel;
using System.Runtime.InteropServices.Marshalling;
using Terr3D.Server.Engine;

namespace Terr3D.Server.Components;
public abstract class SingletonComponent : BehaviourComponent
{
    internal event Action? OnDestroyedAction;
    protected SingletonComponent(params Type[] types)
    {
        List<Component> dependencies = [];
        foreach (var d in types) dependencies.Add((Component)Activator.CreateInstance(d)!);
        World.RegisterSingleton(this, OnDestroyedAction!, [.. dependencies]);
    }

    protected SingletonComponent(params Component[] dependencies)
    {
        World.RegisterSingleton(this, OnDestroyedAction!, dependencies);
    }

    protected SingletonComponent()
    {
        World.RegisterSingleton(this, OnDestroyedAction!, []);
    }


    protected override void OnUpdate(float dt){}

    protected override void OnDestroyed()
    {
        OnDestroyedAction?.Invoke();
    }
}