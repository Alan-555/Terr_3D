namespace Terr3D.Server.Components;

public class PlayerCamera : SingletonComponent
{
    public Camera plrCam;

    public PlayerCamera(Camera plrCam) : base()
    {
        this.plrCam = plrCam;
    }
}