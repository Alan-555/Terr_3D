using Terr3D.Server.Engine;

namespace Terr3D.Server.Components;
public abstract class SingletonComponent
{
    protected SingletonComponent() => World.Register(this);
    public virtual void Update(float dt) { }
}