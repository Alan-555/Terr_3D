using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using Terr3D.Client.Resources;
using Terr3D.Utils;

namespace Terr3D.Client.Rendering;

/// <summary>
/// An instance of a shader's material: a byte buffer mirroring the shader's material uniform block,
/// plus the textures for its samplers. Many materials can share one ShaderProgram.
/// </summary>
public sealed class Material : IDisposable
{
    public readonly ShaderProgram Shader;
 
    /// <summary>Optional per-material override of the shader's render state (e.g. a different cull mode).</summary>
    public RenderState? StateOverride;
 
    readonly MaterialLayout layout;
    readonly byte[] data;
    readonly Texture?[] textures;
    int ubo;
    bool dirty = true;
 
    public Material(ShaderProgram shader)
    {
        Shader = shader;
        layout = shader.Layout;
        data = new byte[layout.BlockSize];
        textures = new Texture?[layout.TextureUnits.Count];
    }
 
    /// <summary>Sets a field of the shader's material struct, e.g. Set("tint", new Vector4(1,0,0,1)).</summary>
    public void Set<T>(string name, T value) where T : unmanaged
    {
        if (!layout.Fields.TryGetValue(name, out var f))
        {
            Diagnostics.Warn($"Material has no field '{name}'");
            return;
        }
        if (Unsafe.SizeOf<T>() > f.size)
        {
            Diagnostics.Warn($"Material field '{name}' is {f.size} bytes, but {typeof(T).Name} is {Unsafe.SizeOf<T>()}");
            return;
        }
        MemoryMarshal.Write(data.AsSpan(f.offset, Unsafe.SizeOf<T>()), in value);
        dirty = true;
    }
 
    public void SetTexture(string name, Texture tex)
    {
        if (!layout.TextureUnits.TryGetValue(name, out int unit))
        {
            Diagnostics.Warn($"Material has no texture '{name}'");
            return;
        }
        textures[unit] = tex;
    }
 
    /// <summary>Binds program, render state, material buffer and textures. Call before drawing.</summary>
    public void Bind()
    {
        Shader.Use();
        (StateOverride ?? Shader.State).Apply();
 
        if (layout.BlockSize > 0)
        {
            if (ubo == 0)
            {
                ubo = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.UniformBuffer, ubo);
                GL.BufferData(BufferTarget.UniformBuffer, data.Length, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            }
            if (dirty)
            {
                GL.BindBuffer(BufferTarget.UniformBuffer, ubo);
                GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, data.Length, data);
                dirty = false;
            }
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, UboBindings.Material, ubo);
        }
 
        for (int unit = 0; unit < textures.Length; unit++)
        {
            if (textures[unit] is not { } tex) continue;
            GL.ActiveTexture(TextureUnit.Texture0 + unit);
            GL.BindTexture(TextureTarget.Texture2D, tex);
        }
    }
 
    public void Dispose()
    {
        if (ubo != 0) { GL.DeleteBuffer(ubo); ubo = 0; }
    }
}


/// <summary>Fixed UBO binding points. Must match the [vk::binding(N)] numbers in shaders.</summary>
public static class UboBindings
{
    public const int Camera   = 0;   // per frame, filled by the renderer
    public const int Object   = 1;   // per draw, filled by the renderer
    public const int Material = 2;   // per material, filled by Material
}


public sealed class MaterialLayout
{
    public int BlockSize; //0 = shader has no material
    public readonly Dictionary<string, (int offset, int size)> Fields = new();
    public readonly Dictionary<string, int> TextureUnits = new();

    public static MaterialLayout Scan(int program)
    {
        var layout = new MaterialLayout();


        GL.GetProgram(program, GetProgramParameterName.ActiveUniformBlocks, out int blockCount);
        int materialBlock = -1;
        for (int b = 0; b < blockCount; b++)
        {
            GL.GetActiveUniformBlock(program, b, ActiveUniformBlockParameter.UniformBlockBinding, out int binding);
            if (binding == UboBindings.Material)
            {
                materialBlock = b;
                GL.GetActiveUniformBlock(program, b, ActiveUniformBlockParameter.UniformBlockDataSize, out layout.BlockSize);
            }
#if DEBUG
            GL.GetActiveUniformBlockName(program, b, 256, out _, out string blockName);
            Diagnostics.Debug($"Shader[{program}] uniform block '{blockName}' at binding {binding}");
#endif
        }

        GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out int uniformCount);
        int nextUnit = 0;

        for (int i = 0; i < uniformCount; i++)
        {
            GL.GetActiveUniform(program, i, 256, out _, out int arraySize, out ActiveUniformType type, out string rawName);

            int[] idx = { i }, blockIdx = new int[1], offset = new int[1];
            GL.GetActiveUniforms(program, 1, idx, ActiveUniformParameter.UniformBlockIndex, blockIdx);

            string name = Normalize(rawName);

            if (blockIdx[0] == -1)
            {
                if (type.ToString().Contains("Sampler"))
                {
                    int loc = GL.GetUniformLocation(program, rawName);
                    if (loc == -1) continue;

                    GL.ProgramUniform1(program, loc, nextUnit);
                    layout.TextureUnits[name] = nextUnit++;
                }
                continue;
            }

            if (blockIdx[0] != materialBlock) continue;

            GL.GetActiveUniforms(program, 1, idx, ActiveUniformParameter.UniformOffset, offset);

            int size = SizeOf(type);
            if (size == 0 || arraySize > 1)
            {
                Diagnostics.Warn($"Material field '{rawName}' ({type}, array size {arraySize}) is not supported, skipped");
                continue;
            }

            layout.Fields[name] = (offset[0], size);
        }

        if (layout.BlockSize == 0 && blockCount > 0)
            Diagnostics.Debug($"Shader[{program}] has uniform blocks but none at binding {UboBindings.Material}; no material block");

        return layout;
    }

    static string Normalize(string name)
    {
        int dot = name.LastIndexOf('.');
        if (dot >= 0) name = name[(dot + 1)..];

        name = name.Replace("[0]", "");

        int us = name.LastIndexOf('_');
        if (us > 0 && us < name.Length - 1 && name.AsSpan(us + 1).IndexOfAnyExceptInRange('0', '9') < 0)
            name = name[..us];

        return name;
    }
    static int SizeOf(ActiveUniformType t) => t switch
    {
        ActiveUniformType.Float or ActiveUniformType.Int or ActiveUniformType.UnsignedInt or ActiveUniformType.Bool => 4,
        ActiveUniformType.FloatVec2 or ActiveUniformType.IntVec2 or ActiveUniformType.UnsignedIntVec2 or ActiveUniformType.BoolVec2 => 8,
        ActiveUniformType.FloatVec3 or ActiveUniformType.IntVec3 or ActiveUniformType.UnsignedIntVec3 or ActiveUniformType.BoolVec3 => 12,
        ActiveUniformType.FloatVec4 or ActiveUniformType.IntVec4 or ActiveUniformType.UnsignedIntVec4 or ActiveUniformType.BoolVec4 => 16,
        ActiveUniformType.FloatMat4 => 64,
        _ => 0,
    };
}