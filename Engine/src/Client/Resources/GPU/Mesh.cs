using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Terr3D.Client.Resources;

/// <summary>
/// Defines a VBO, IBO and VAO to store mesh data. The data is kept in the RAM as well, should the CPU want to access it
/// </summary>
public class Mesh
{
    Vertex[] vertices;
    public Triangle[] triangles;

    public VBO VBO;
    public IBO IBO;
    public VAO VAO;

    public Vector3 minPoint, maxPoint, center, halfExtents;

    public Mesh(Vertex[] data, Triangle[] tridata)
    {
        vertices = data;
        triangles = tridata;

        //calculate the model space min and max points
        foreach (var vertex in vertices)
        {
            var pos = vertex.Position;
            if (pos.X < minPoint.X)
                minPoint.X = pos.X;
            if (pos.Y < minPoint.Y)
                minPoint.Y = pos.Y;
            if (pos.Z < minPoint.Z)
                minPoint.Z = pos.Z;

            if (pos.X > maxPoint.X)
                maxPoint.X = pos.X;
            if (pos.Y > maxPoint.Y)
                maxPoint.Y = pos.Y;
            if (pos.Z > maxPoint.Z)
                maxPoint.Z = pos.Z;
        }


        center = (maxPoint + minPoint) * 0.5f;
        halfExtents = (maxPoint - minPoint) * 0.5f;

        VBO = new(vertices);
        IBO = new(triangles);
        VAO = new(VBO, IBO);
    }
}


/// <summary>
/// A vertex buffer object
/// </summary>
public class VBO : GPU_Resource
{
    public VBO(Vertex[] vertices)
    {
        _resHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _resHandle);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Vertex.SizeInBytes, vertices, BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    public override void Dispose()
    {
        if (_resHandle != 0)
            GL.DeleteBuffer(this);
        base.Dispose();
    }
}

/// <summary>
/// An index buffer object - stores indices to vertices
/// </summary>
public class IBO : GPU_Resource
{
    public IBO(Triangle[] triangles)
    {
        _resHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, this);
        GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Length * 3 * sizeof(int), triangles, BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }

    public override void Dispose()
    {
        if (_resHandle != 0)
            GL.DeleteBuffer(this);
        base.Dispose();
    }
}

/// <summary>
/// A vertex array object to pack all vertex attributes together
/// </summary>
public class VAO : GPU_Resource
{
    public VAO(VBO vbo, IBO ibo)
    {
        _resHandle = GL.GenVertexArray();
        GL.BindVertexArray(this);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

        //Position
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, 0);
        GL.EnableVertexAttribArray(0);

        //Normal
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, Marshal.OffsetOf<Vertex>("Normal"));
        GL.EnableVertexAttribArray(1);

        //Texture UV
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, Marshal.OffsetOf<Vertex>("TextUV"));
        GL.EnableVertexAttribArray(2);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
        GL.BindVertexArray(0);
    }

    public override void Dispose()
    {
        if (_resHandle != 0)
            GL.DeleteVertexArray(this);
        base.Dispose();
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TextUV;

    public Vertex(Vector3 position, Vector3 normal, Vector2 textUV)
    {
        Position = position;
        Normal = normal;
        TextUV = textUV;
    }

    public static int SizeInBytes => Marshal.SizeOf<Vertex>();
}

[StructLayout(LayoutKind.Sequential)] public struct Triangle { public uint i0, i1, i2; }