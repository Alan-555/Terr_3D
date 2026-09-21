
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
/// Font material used to render text to the screen
/// </summary>
public class FontShaderMaterial : Material
{
    public Vector4 uvRect;
    public Vector3 colour = Vector3.One;

    public Matrix4 model;

    public required Texture texture;


    public override void SetUniforms(ShaderProgram shader)
    {
        shader.SetUniform("uvRect", ref uvRect);
        shader.SetUniform("model", ref model);
        shader.SetUniform("colour", ref colour);
        shader.SetUniform("albedo",ref texture, 0);
    }
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

public class ScreenTextureMaterial : Material
{
    public Texture text;
    public Vector2 pos;
    public Vector4 colour;

    public override void SetUniforms(ShaderProgram shader)
    {
        shader.SetUniform("text", ref text, 0);
        shader.SetUniform("pos", ref pos);
        shader.SetUniform("colour", ref colour);
    }
}