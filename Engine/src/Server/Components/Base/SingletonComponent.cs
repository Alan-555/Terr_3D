using System.Collections.ObjectModel;
using System.Runtime.InteropServices.Marshalling;
using Terr3D.Server.Core;

namespace Terr3D.Server.Components;
public abstract class SingletonComponent : BehaviourComponent
{
    internal event Action? OnDestroyedAction;

    public void Register()
    {
        World.RegisterSingleton(this, OnDestroyedAction!, []);
    }

//TODO: think of what to do with dependencies. They cannot be done like this since the user does not know which ones to pass
   /* public void Register(params Component[] dependencies)
    {
        World.RegisterSingleton(this, OnDestroyedAction!, dependencies);
    }

    public void Register(params Type[] types)
    {
        List<Component> dependencies = [];
        foreach (var d in types) dependencies.Add((Component)Activator.CreateInstance(d)!);
        World.RegisterSingleton(this, OnDestroyedAction!, [.. dependencies]);
    }

*/
    protected override void OnUpdate(float dt){}

    protected override void OnDestroyed()
    {
        OnDestroyedAction?.Invoke();
    }
}