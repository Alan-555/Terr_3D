using OpenTK.Mathematics;
using Terr3D.Client.Resources;

namespace Terr3D.Server.Shared;

public class FlatColourMaterial : Material
{
    public Vector3 colour = Vector3.One;

    public override void SetUniforms(ShaderProgram shader)
    {
        shader.SetUniform("colour", ref colour);
    }
}