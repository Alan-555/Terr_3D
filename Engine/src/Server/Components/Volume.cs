using OpenTK.Mathematics;
using Terr3D.Client.Resources;
using Terr3D.Server.Shared;
using Terr3D.Utils;

namespace Terr3D.Server.Components;

public class VolumeComponent : Component
{
    public Bounds Volume {get; private set;}

}