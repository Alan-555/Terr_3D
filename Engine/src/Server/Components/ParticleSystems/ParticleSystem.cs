using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Terr3D.Client.Resources;
using Terr3D.Server.Shared;
using Terr3D.Utils;

namespace Terr3D.Server.Components;

public abstract class ParticleSystem : BehaviourComponent
{
    protected ParticleInstanceData[] _instances;
    private int _instanceVBO;
    private int _vao;
    private Mesh _baseMesh;
    private ShaderProgram _shader;
    private Texture _texture;

    protected int _count;

    protected Vector4 colour = Vector4.One;
    

    public ParticleSystem(int numParticles, Texture texture)
    {
        _count = numParticles;
        _instances = new ParticleInstanceData[numParticles];
        _baseMesh = ResourceManager.Meshes[ResourceIndex.Meshes.Quad];
        _shader = ResourceManager.Shaders[ResourceIndex.Shaders.Particle];
        _texture = texture;

        InitGPU();
    }

    private void InitGPU()
    {
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        //bind the base mesh VBO
        GL.BindBuffer(BufferTarget.ArrayBuffer, _baseMesh.VBO);

        //particle pos
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, 0);

        //normal, practically not needed but technically it is
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, Marshal.OffsetOf<Vertex>("Normal"));

        //text uv
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, Marshal.OffsetOf<Vertex>("TextUV"));

        //index buffer object of the mesh
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _baseMesh.IBO);

        //instance VBO
        _instanceVBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, _count * Marshal.SizeOf<ParticleInstanceData>(), IntPtr.Zero, BufferUsageHint.StreamDraw);

        //instance attrib
        //iOffset
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf<ParticleInstanceData>(), 0);
        GL.VertexAttribDivisor(3, 1);

        //iScale
        GL.EnableVertexAttribArray(4);
        GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf<ParticleInstanceData>(), (IntPtr)Marshal.OffsetOf<ParticleInstanceData>("Scale"));
        GL.VertexAttribDivisor(4, 1);

        //iAlpha
        GL.EnableVertexAttribArray(5);
        GL.VertexAttribPointer(5, 1, VertexAttribPointerType.Float, false, Marshal.SizeOf<ParticleInstanceData>(), (IntPtr)Marshal.OffsetOf<ParticleInstanceData>("Alpha"));
        GL.VertexAttribDivisor(5, 1);

        GL.BindVertexArray(0);
    }


    

    public void Render(ref Matrix4 view, ref Matrix4 projection, Vector3 camPos)
    {
        if (!IsEnabled) return;

        _shader.Use();
        _shader.SetUniform("view", ref view);
        _shader.SetUniform("projection", ref projection);
        Texture tex = _texture;
        _shader.SetUniform("albedo", ref tex, 0);
        _shader.SetUniform("colour", ref colour, 0);
        _shader.SetUniform("camPos", ref camPos, 0);

        GL.BindVertexArray(_vao);

        //upload per-instance data
        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _count * Marshal.SizeOf<ParticleInstanceData>(), _instances);

        GL.Disable(EnableCap.CullFace);
        GL.DepthMask(false);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.DrawElementsInstanced(PrimitiveType.Triangles, _baseMesh.triangles.Length * 3, DrawElementsType.UnsignedInt, IntPtr.Zero, _count);

        GL.DepthMask(true);
        GL.Enable(EnableCap.CullFace);
        GL.Disable(EnableCap.Blend);
        GL.BindVertexArray(0);
    }

    protected override void OnDestroyed()
    {
        GL.DeleteBuffer(_instanceVBO);
        GL.DeleteVertexArray(_vao);
    }
}


[StructLayout(LayoutKind.Sequential)]
public struct ParticleInstanceData
{
    public Vector3 Position;
    public Vector3 Scale;
    public float Alpha;
    public Vector3 Colour;
}