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

    public ISupportsWireframe Target => _wireframeShape;
    private ISupportsWireframe _wireframeShape;

    public WireframeBoxRenderer(ISupportsWireframe wireframe, Vector3 colour, bool isStatic = true) : base(ResourceManager.Shaders[ResourceIndex.Shaders.FlatColor], ResourceManager.Meshes[ResourceIndex.Meshes.Cube], isStatic ? RendererClass.RENDERER_STATIC : RendererClass.RENDERER_DYNAMIC)
    {
        material = new FlatColourMaterial()
        {
            colour = colour
        };
        _wireframeShape = wireframe;
        SetEnabled(false);

    }
}


public interface ISupportsWireframe
{
    public Vector3 GetPos();
    public Quaternion GetRot();
    public Vector3 GetScale();
}