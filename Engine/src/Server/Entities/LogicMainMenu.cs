using OpenTK.Windowing.GraphicsLibraryFramework;
using Terr3D.Client;
using Terr3D.Server.Shared;
using Terr3D.Server.World;

namespace Terr3D.Server.Entities;

public class LogicMainMenu : Entity, IUpdates
{
    public LogicMainMenu(Worldspawn worldspawn) : base("MainMenu", worldspawn, true)
    {
        
    }

    public override void OnUpdate(float dt)
    {
        if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.Space))
        {
            Onstage.DestroyScene();
            Program.NewScene();
        }
    }


}