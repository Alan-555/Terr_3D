using OpenTK.Mathematics;
using Terr3D.Client.Resources;
using Terr3D.Server.Shared;
using Terr3D.Utils;

namespace Terr3D.Server.Components;

/// <summary>
/// Renders a mesh with the specified shader and optional material
/// </summary>
/// <param name="shader"></param>
/// <param name="mesh"></param>
public class Renderer(ShaderProgram shader, Mesh mesh, RendererClass rendererClass = RendererClass.RENDERER_DYNAMIC) : Component, ISpatialBounds
{
    public ShaderProgram ShaderProgram { get; set; } = shader;
    public Mesh mesh = mesh;
    public int TriangleCount => mesh.triangles.Length;

    public readonly RendererClass rendererClass = rendererClass;

    /// <summary>
    /// The material applied to this renderer
    /// </summary>
    public Material? material;

    /// <summary>
    /// The static bounding box of this entity. Requires to be calculated prior!
    /// </summary>
    private Bounds _staticBoundingBox;


    /// <summary>
    /// What frame this entity was rendered on? Used to ensure that each renderer is drawn exactly once each frame
    /// </summary>
    public uint renderedOn = 1;

    protected override void OnInitialise()
    {
        ComputeStaticBB();
        if (Entity.IsStatic && this is not WireframeBoxRenderer && rendererClass == RendererClass.RENDERER_DYNAMIC)
            Diagnostics.Warn("Creating a dynamic renderer on a static object!");
        if (!Entity.IsStatic && rendererClass == RendererClass.RENDERER_STATIC)
            Diagnostics.Error("Creating a static renderer on a dynamic object!");
    }

    /// <summary>
    /// Computes and caches the axis aligned bounding box. Supports all transformations
    /// </summary>
    public void ComputeStaticBB()
    {
        var modelMin = mesh.minPoint;
        var modelMax = mesh.maxPoint;

        //reconstruct all 8 corners
        Vector3[] corners =
        [
            new Vector3(modelMax.X, modelMax.Y, modelMax.Z),
            new Vector3(modelMax.X, modelMax.Y, modelMin.Z),
            new Vector3(modelMax.X, modelMin.Y, modelMax.Z),
            new Vector3(modelMax.X, modelMin.Y, modelMin.Z),
            new Vector3(modelMin.X, modelMax.Y, modelMax.Z),
            new Vector3(modelMin.X, modelMax.Y, modelMin.Z),
            new Vector3(modelMin.X, modelMin.Y, modelMax.Z),
            new Vector3(modelMin.X, modelMin.Y, modelMin.Z),
        ];

        //designate one point as the max and min
        Vector3 firstWorldPoint = (new Vector4(corners[0], 1) * Transform.GlobalMatrix).Xyz;
        Vector3 worldMin = firstWorldPoint;
        Vector3 worldMax = firstWorldPoint;

        //inflate using the rest
        for (int i = 1; i < 8; i++)
        {
            Vector3 pos = (new Vector4(corners[i], 1) * Transform.GlobalMatrix).Xyz;

            if (pos.X < worldMin.X)
                worldMin.X = pos.X;
            if (pos.Y < worldMin.Y)
                worldMin.Y = pos.Y;
            if (pos.Z < worldMin.Z)
                worldMin.Z = pos.Z;

            if (pos.X > worldMax.X)
                worldMax.X = pos.X;
            if (pos.Y > worldMax.Y)
                worldMax.Y = pos.Y;
            if (pos.Z > worldMax.Z)
                worldMax.Z = pos.Z;
        }

        _staticBoundingBox = Bounds.FromTwoPoints(worldMin, worldMax);
    }

    private Bounds? _cachedDynamicAABB = null;

    public Bounds DynamicAABB
    {
        get
        {
            if(_cachedDynamicAABB != null) return _cachedDynamicAABB.Value;
            var localCenter = mesh.center;
            var localExtents = mesh.halfExtents;
            var matrix = Transform.GlobalMatrix;

            //transform the world center
            Vector3 worldCenter = (new Vector4(localCenter, 1.0f) * matrix).Xyz;

            //Transform the extends using the transformation matrix
            Vector3 worldExtents = new(
                Math.Abs(matrix.M11) * localExtents.X + Math.Abs(matrix.M21) * localExtents.Y + Math.Abs(matrix.M31) * localExtents.Z,
                Math.Abs(matrix.M12) * localExtents.X + Math.Abs(matrix.M22) * localExtents.Y + Math.Abs(matrix.M32) * localExtents.Z,
                Math.Abs(matrix.M13) * localExtents.X + Math.Abs(matrix.M23) * localExtents.Y + Math.Abs(matrix.M33) * localExtents.Z
            );

            //Reconstruct the world min and max and return
            Vector3 worldMin = worldCenter - worldExtents;
            Vector3 worldMax = worldCenter + worldExtents;

            return (_cachedDynamicAABB = Bounds.FromTwoPoints(worldMin, worldMax)).Value;
        }
    }

    public Bounds GetBounds()
    {
        if(rendererClass == RendererClass.RENDERER_STATIC) return _staticBoundingBox;
        return DynamicAABB;
    }

    /// <summary>
    /// Applies a fresh paint to this object
    /// </summary>
    public void ApplyMaterial()
    {
        if (material == null) return;

        material.SetUniforms(ShaderProgram);
    }

    protected override void OnTransformUpdate()
    {
        //invalidate
        _cachedDynamicAABB = null;
    }
}


public enum RendererClass
{
    /// <summary>
    /// Dynamic renderers that may change transformation each frame
    /// </summary>
    RENDERER_DYNAMIC,

    /// <summary>
    /// Static renderers that are static and are baked in the quadtree
    /// </summary>
    RENDERER_STATIC,

    /// <summary>
    /// Renderers that render to the UI
    /// </summary>
    RENDERER_UI,

    /// <summary>
    /// A dynamic renderer that skips any culling
    /// </summary>
    RENDERER_PERSISTENT,

    /// <summary>
    /// Handled specially, never renderer by the sceneDrawer directly
    /// </summary>
    RENDER_IGNORE,
}