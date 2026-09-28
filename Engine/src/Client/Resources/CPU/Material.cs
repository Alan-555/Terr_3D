
using OpenTK.Mathematics;

namespace Terr3D.Client.Resources;

/// <summary>
/// Base class for any material
/// </summary>
public abstract class Material
{
    /// <summary>
    /// Function that will set the uniforms
    /// </summary>
    /// <param name="shader"></param>
    public abstract void SetUniforms(ShaderProgram shader);
}


/// <summary>
/// For the shaded shader
/// </summary>
public class ShadedMaterial : Material
{

    public Vector3 diffuse, specular;
    public float shininess;

    public Texture albedo = ResourceManager.Textures[ResourceIndex.Textures.EmptyWhite];

    public override void SetUniforms(ShaderProgram shader)
    {
        shader.SetUniform("material.diffuse", ref diffuse);
        shader.SetUniform("material.specular", ref specular);
        shader.SetUniform("material.shininess", ref shininess);
        shader.SetUniform("material.albedo", ref albedo, 0);
    }
}