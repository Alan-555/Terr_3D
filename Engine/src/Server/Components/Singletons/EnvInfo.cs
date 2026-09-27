using OpenTK.Mathematics;
using Terr3D.Client;
using Terr3D.Client.Resources;

namespace Terr3D.Server.Components;

public class EnvConfig : SingletonComponent
{
    public static readonly ShaderProgram BuiltinSky = ResourceManager.Shaders[ResourceIndex.Shaders.Sky];
    public Renderer SkyRenderer {get; private set;}

    public Quaternion SunRotation {get; set;} = Quaternion.FromEulerAngles(new Vector3(-0.7f,0f,0f));

    public Vector3 SunDir => Vector3.Transform(Vector3.UnitZ, SunRotation);

    public float FogDensity {get; set;} = 0.001f;

    public EnvConfig(ShaderProgram skyShader)
    {
       SkyRenderer = new Renderer(skyShader, ResourceManager.Meshes[ResourceIndex.Meshes.SkyQuad], RendererClass.RENDER_IGNORE);
    }

    public override void Update(float dt)
    {
        if(!Program.DEBUG_FLAG) return;
        if (Input.KeyDown(KeyCode.PageUp))
        {
            SunRotation *= Quaternion.FromAxisAngle(Vector3.UnitX, 0.5f * dt);
        }

        if (Input.KeyDown(KeyCode.PageDown))
        {
            SunRotation *= Quaternion.FromAxisAngle(Vector3.UnitX, -0.5f * dt);
        }
    }
}