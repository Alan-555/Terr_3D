using System.Collections.ObjectModel;
using System.Runtime.InteropServices.Marshalling;
using Terr3D.Server.Engine;

namespace Terr3D.Server.Components;
public abstract class SingletonComponent : BehaviourComponent
{
    protected SingletonComponent(params Type[] types)
    {
        List<Component> dependencies = [];
        foreach (var d in types) dependencies.Add((Component)Activator.CreateInstance(d)!);
        World.RegisterSingleton(this, _destroyed!, [.. dependencies]);
    }

    protected SingletonComponent(params Component[] dependencies)
    {
        World.RegisterSingleton(this, _destroyed!, dependencies);
    }

    protected SingletonComponent()
    {
        World.RegisterSingleton(this, _destroyed!, []);
    }

    public event Action? _destroyed;

    public override void Update(float dt){}

    public new void Destroy()
    {
        base.Destroy();
        _destroyed?.Invoke();
    }
}