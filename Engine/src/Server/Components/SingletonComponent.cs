using System.Collections.ObjectModel;
using System.Runtime.InteropServices.Marshalling;
using Terr3D.Server.Engine;

namespace Terr3D.Server.Components;
public abstract class SingletonComponent : BehaviourComponent
{
    public ReadOnlyCollection<Type> Dependencies => _dependencies.AsReadOnly();
    private Type[] _dependencies;
    protected SingletonComponent(params Type[] dependencies)
    {
        _dependencies = dependencies;
        World.RegisterSingleton(this, _destroyed!);
    }

    public event Action? _destroyed;

    public override void Update(float dt){}

    public new void Destroy()
    {
        base.Destroy();
        _destroyed?.Invoke();
    }
}