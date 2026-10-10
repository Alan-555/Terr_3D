using Terr3D.Server.Core;

namespace Terr3D.Server.Components;

public class Ground(IGroundProvider groundProvider) : Component
{
    public readonly IGroundProvider groundProvider = groundProvider;

    protected override void OnInitialise()
    {
        base.OnInitialise();
        World.Ground.Register(groundProvider);
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        World.Ground.Unregister(groundProvider);
    }
}