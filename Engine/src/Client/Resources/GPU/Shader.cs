using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using Terr3D.Utils;

namespace Terr3D.Client.Resources;

/// <summary>
/// A class for a shader
/// </summary>
public class Shader : GPU_Resource
{
    public Shader(ShaderType shaderType, string src)
    {
        //Create and compile the shader
        _resHandle = GL.CreateShader(shaderType);
        GL.ShaderSource(this, src);
        GL.CompileShader(this);
        //check status
        GL.GetShader(this, ShaderParameter.CompileStatus, out int vStatus);
        if (vStatus == 0)
        {
            string log = GL.GetShaderInfoLog(this);
            Diagnostics.Error($"SHADER ERROR: {log}");
            throw new Exception(log);
        }

    }

    public override void Dispose()
    {
        if (_resHandle != 0)
            GL.DeleteShader(this);
        base.Dispose();
    }
}

/// <summary>
/// A class for a generic shader program. Links multiple shaders together and then disposes of them!!
/// </summary>
public class ShaderProgram : GPU_Resource
{

    /// <summary>
    /// These flags describe what uniforms does the shader want and the behaviour associated with it
    /// </summary>
    public CommonBehaviourFlags behaviourFlags = CommonBehaviourFlags.NONE;

    Dictionary<string, int> uniforms = new();

    public ShaderProgram(CommonBehaviourFlags flags, params Shader[] shaders)
    {
        behaviourFlags = flags;
        //Create the program and compile all shaders
        _resHandle = GL.CreateProgram();
        foreach (var sh in shaders)
        {
            GL.AttachShader(this, sh);
        }
        GL.LinkProgram(this);

        //Dispose of the shaders
        foreach (var sh in shaders)
        {
            GL.DetachShader(this, sh);
            sh.Dispose();
        }

        //Check status
        GL.GetProgram(this, GetProgramParameterName.LinkStatus, out int success);

        if (success == 0)
        {
            string log = GL.GetProgramInfoLog(this);
            Diagnostics.Error($"SHADER PROGRAM ERROR: {log}");
            throw new Exception(log);
        }
        //Pre-load the uniform indices
        LoadUniforms();
    }

    private void LoadUniforms()
    {
        GL.GetProgram(this, GetProgramParameterName.ActiveUniforms, out var numUniforms);
        for (int i = 0; i < numUniforms; i++)
        {
            GL.GetActiveUniform(this, i, 256, out _, out _, out _, out var name);
            int loc = GL.GetUniformLocation(this, name);
            if (loc != -1)
            {
                uniforms[name] = loc;
                Diagnostics.Debug($"Shader[{_resHandle}] loaded uniform {name} with location of {loc}");
            }

        }
    }

    public void Use()
    {
        GL.UseProgram(this);
    }
    public void SetUniformNoRef<T>(string name, T value, int unit = -1)
    {
        T val = value;
        SetUniform(name, ref val, unit);
    }
    /// <summary>
    /// Sets a specific uniform
    /// </summary>
    /// <typeparam name="T">The CPU type of the uniform to be set</typeparam>
    /// <param name="name">The name of the uniform to be set</param>
    /// <param name="value">The value to set the uniform to</param>
    public void SetUniform<T>(string name, ref T value, int unit = -1)
    {
        if (!uniforms.TryGetValue(name, out var location))
        {
            Diagnostics.Warn($"Setting unknown uniform {name} for ShaderProgram {_resHandle}");
        }
        switch (value)
        {
            case int v:
                GL.Uniform1(location, v);
                break;
            case float v:
                GL.Uniform1(location, v);
                break;
            case Vector2 v:
                GL.Uniform2(location, ref v);
                break;
            case Vector3 v:
                GL.Uniform3(location, ref v);
                break;
            case Vector4 v:
                GL.Uniform4(location, ref v);
                break;
            case Matrix4 v:
                GL.UniformMatrix4(location, false, ref v);
                break;
            case Texture v:
                SetTexture(location, v, unit);
                break;

        }
    }

    void SetTexture(int location, Texture tex, int unit)
    {
        if (unit == -1)
        {
            throw new ArgumentException("Unit was not specified!");
        }
        GL.ActiveTexture(TextureUnit.Texture0 + unit);

        GL.BindTexture(TextureTarget.Texture2D, tex);

        GL.Uniform1(location, unit);
    }

    public override void Dispose()
    {
        if (_resHandle != 0)
            GL.DeleteProgram(this);
        base.Dispose();
    }
}

[Flags]
public enum CommonBehaviourFlags
{
    /// <summary>
    /// No behaviour for this shader
    /// </summary>
    NONE = 0,
    /// <summary>
    /// Provides  model, view and projection matrices uniforms
    /// </summary>
    TRANSFORM_TO_NDC = 1,
    /// <summary>
    /// Reserved by the sky shader
    /// </summary>
    USE_SKY = 2,
    /// <summary>
    /// Should this use orthographic projection? Does not transform to world space, nor view space
    /// </summary>
    USE_ORTHO_PROJ = 4,
    /// <summary>
    /// Provides vec3 SunDir uniform
    /// </summary>
    USE_SUN_DIR = 8,
    /// <summary>
    /// Provides inverse of the view matrix
    /// </summary>
    USE_INV_PROJ_VIEW = 16,
    /// <summary>
    /// Provides vec3 camPos - the camera world position
    /// </summary>
    USE_CAMERA_POS = 32,
    /// <summary>
    /// Uses alpha blending
    /// </summary>
    USE_ALPHA_BLEND = 64,

    /// <summary>
    /// Provides all the dynamic lights of a scene
    /// </summary>
    USE_DYNAMIC_LIGHTS = 128,

    /// <summary>
    /// Renders the object in wireframe mode
    /// </summary>
    USE_WIREFRAME = 256,

    /// <summary>
    /// Renders on top of everything
    /// </summary>
    USE_RENDER_PRIORITY = 512,

    /// <summary>
    /// Provides a model matrix uniform
    /// </summary>
    USE_MODEL_MATRIX = 1024,

    /// <summary>
    /// Provides the shadow map
    /// </summary>
    USE_SHADOW_MAP = 2048,

    /// <summary>
    /// Provides uniforms required for the fog
    /// </summary>
    USE_SKY_FOG = 4096,

    /// <summary>
    /// Provides uniforms for various weather effects
    /// </summary>
    USE_WEATHER_EFFECTS = 8192,
}