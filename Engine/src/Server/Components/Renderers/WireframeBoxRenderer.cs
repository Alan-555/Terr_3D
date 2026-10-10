using OpenTK.Mathematics;
using Terr3D.Client.Resources;
using Terr3D.Server.Shared;
using Terr3D.Utils;

namespace Terr3D.Server.Components;

/// <summary>
/// A debug component that renders a wireframe box tracking a specific target.
/// </summary>
public class WireframeBoxRenderer : Renderer
{
    public static Vector3 ColourCollider = new(0, 1, 0);
    public static Vector3 ColourCloudPoison = new(1, 0, 0);

    public ISpatialBounds Target => _wireframeShape;
    private ISpatialBounds _wireframeShape;

    public WireframeBoxRenderer(ISpatialBounds wireframe, Vector3 colour, bool isStatic = true) : base(null, null, isStatic ? RendererClass.RENDERER_STATIC : RendererClass.RENDERER_DYNAMIC) //TODO: fix. FlatColour and Cube
    {
        material = new FlatColourMaterial()
        {
            colour = colour
        };
        _wireframeShape = wireframe;
        //SetEnabled(false);

    }

    public void GetWireframeData(Bounds bounds, out float[] vertices, out uint[] indices)
    {
        Vector3 min = bounds.MinPoint;
        Vector3 max = bounds.MaxPoint;

        vertices = new float[]
        {
        min.X, min.Y, min.Z,
        max.X, min.Y, min.Z,
        max.X, max.Y, min.Z,
        min.X, max.Y, min.Z,        
        min.X, min.Y, max.Z, 
        max.X, min.Y, max.Z, 
        max.X, max.Y, max.Z, 
        min.X, max.Y, max.Z 
        };

        indices = new uint[]
        {
        //front face
        0, 1,   1, 2,   2, 3,   3, 0, 
        //back face
        4, 5,   5, 6,   6, 7,   7, 4, 
        //other edges
        0, 4,   1, 5,   2, 6,   3, 7
        };
    }
}