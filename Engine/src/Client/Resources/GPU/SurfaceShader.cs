using OpenTK.Graphics.OpenGL4;

namespace Terr3D.Client.Resources;

/// <summary>
/// A wrapper class for a "surface" shader. Takes a source for both a vertex and a fragment shader, compiles them and links them.
/// </summary>
public class SurfaceShader : ShaderProgram
{
    public SurfaceShader(string vertexSrc, string fragSrc, CommonBehaviourFlags flags) : base(flags, new Shader(ShaderType.VertexShader, vertexSrc), new Shader(ShaderType.FragmentShader, fragSrc))
    {
    }
}