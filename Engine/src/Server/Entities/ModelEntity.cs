using OpenTK.Mathematics;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.World;
using Terr3D.Utils;

namespace Terr3D.Server.Entities;

/// <summary>
/// Represent a physical entity in the world
/// </summary>
public class ModelEntity : Entity
{
    public Renderer Renderer {get; private set;}
    public ModelEntity(Scene scene, string name, ShaderProgram shader, Mesh mesh, bool isStatic = false) : base(scene, name, isStatic)
    {
        Renderer = AddComponent(new Renderer(shader, mesh, isStatic ? RendererClass.RENDERER_STATIC : RendererClass.RENDERER_DYNAMIC));
    }

    public ModelEntity(Scene scene, ModelDefinition definition) : base(scene, $"{definition.Name}$", definition.Properties.IsStatic)
    {
        Mesh mesh = ResourceManager.Meshes[definition.Mesh];

        if (mesh == null)
        {
            Diagnostics.Error($"Failed to find mesh '{definition.Mesh}' for model '{definition.Name}'");
            //Fallback to a cube so we don't crash
            mesh = ResourceManager.Meshes[ResourceIndex.Meshes.Cube];
        }
        
        Renderer = AddComponent(new Renderer(ResourceManager.Shaders[definition.Material.Shader], mesh, definition.Properties.IsStatic ? RendererClass.RENDERER_STATIC : RendererClass.RENDERER_DYNAMIC));

        var mat = definition.Material.Properties;

        Vector3 diffuse = new(mat.Diffuse[0], mat.Diffuse[1], mat.Diffuse[2]);
        Vector3 specular = new(mat.Specular[0], mat.Specular[1], mat.Specular[2]);

        Renderer.material = new ShadedMaterial()
        {
            albedo = ResourceManager.Textures[mat.Albedo],
            diffuse = diffuse,
            specular = specular,
            shininess = mat.Shininess,

        };
        
        if (definition.Colliders != null)
        {
            foreach (var col in definition.Colliders)
            {
                if (col.Type == "box")
                {
                    Vector3 center = new(col.Center[0], col.Center[1], col.Center[2]);
                    Vector3 halfExtents = new(col.HalfExtents[0], col.HalfExtents[1], col.HalfExtents[2]);
                    var c = AddComponent(new BoxCollider(halfExtents * 2, center, definition.Properties.IsStatic));
                }
            }
        }
    }

    public void ComputeStaticBB()
    {
        Renderer.ComputeStaticBB();
        /*var e = new ModelEntity(Onstage, "", Renderer.ShaderProgram,ResourceManager.Meshes.cube);
        Onstage.Globals.RegisterEntity(e);
        e.Transform.Position = Renderer.staticBoundingBox.Position;
        e.Transform.Scale = Renderer.staticBoundingBox.HalfExtents * 2;*/
    }
}