using Terr3D.Client.Resources;

namespace Terr3D.Server.Components;

public class SkyFeature() : SingletonComponent(new Renderer(ResourceManager.Shaders[ResourceIndex.Shaders.Sky], ResourceManager.Meshes[ResourceIndex.Meshes.SkyQuad], RendererClass.RENDER_IGNORE))
{
    [field: Dependency]
    public Renderer SkyRenderer {get; private set;}
}