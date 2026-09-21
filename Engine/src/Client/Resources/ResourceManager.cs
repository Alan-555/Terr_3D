using OpenTK.Graphics.OpenGL4;
using Terr3D.Client.Resources.Utils;
using Terr3D.Utils;
using YamlDotNet.Serialization;

namespace Terr3D.Client.Resources;

/// <summary>
/// Responsible for loading GPU assets (shaders, meshes and textures) and providing static access to them
/// </summary>
public static class ResourceManager
{
    const string rootPath = "res/";

    public static RegistryOfResources<SurfaceShader> Shaders { get; } = new();
    public static RegistryOfResources<Mesh> Meshes { get; } = new();
    public static RegistryOfResources<Texture> Textures { get; } = new();
    public static RegistryOfResources<ModelDefinition> Models { get; } = new();
    public static RegistryOfResources<int> Audio { get; } = new();

    /// <summary>
    /// Load all resources
    /// </summary>
    public static void InitialiseGPUResources()
    {
        Diagnostics.Info("Loading and compiling shaders...");
        InitShaders();
        
        Diagnostics.Info("Discovering and loading meshes...");
        LoadMeshes();
        
        Diagnostics.Info("Discovering and loading textures...");
        LoadTextures();

        Diagnostics.Info("Discovering and parsing model definitions...");
        LoadModels();

        Diagnostics.Info("Discovering and loading audio...");
        LoadAudio();
    }

    static void InitShaders()
    {
        Shaders.Register(ResourceIndex.Shaders.Shaded, ShaderLoader.LoadSurfaceShader("Shaded", CommonBehaviourFlags.TRANSFORM_TO_NDC | CommonBehaviourFlags.USE_SUN_DIR | CommonBehaviourFlags.USE_CAMERA_POS | CommonBehaviourFlags.USE_DYNAMIC_LIGHTS | CommonBehaviourFlags.USE_SHADOW_MAP | CommonBehaviourFlags.USE_SKY_FOG | CommonBehaviourFlags.USE_WEATHER_EFFECTS));
        Shaders.Register(ResourceIndex.Shaders.Sky, ShaderLoader.LoadSurfaceShader("Sky", CommonBehaviourFlags.USE_SKY | CommonBehaviourFlags.USE_SUN_DIR | CommonBehaviourFlags.USE_INV_PROJ_VIEW | CommonBehaviourFlags.USE_WEATHER_EFFECTS));
        Shaders.Register(ResourceIndex.Shaders.Font, ShaderLoader.LoadSurfaceShader("Font", CommonBehaviourFlags.USE_ORTHO_PROJ));
        Shaders.Register(ResourceIndex.Shaders.Volume, ShaderLoader.LoadSurfaceShader("Volume", CommonBehaviourFlags.TRANSFORM_TO_NDC | CommonBehaviourFlags.USE_CAMERA_POS | CommonBehaviourFlags.USE_ALPHA_BLEND));
        Shaders.Register(ResourceIndex.Shaders.FlatColor, ShaderLoader.LoadSurfaceShader("FlatColor", CommonBehaviourFlags.USE_WIREFRAME));
        Shaders.Register(ResourceIndex.Shaders.Depth, ShaderLoader.LoadSurfaceShader("Depth", CommonBehaviourFlags.USE_MODEL_MATRIX));
        Shaders.Register(ResourceIndex.Shaders.ScreenTexture, ShaderLoader.LoadSurfaceShader("ScreenText", CommonBehaviourFlags.USE_RENDER_PRIORITY | CommonBehaviourFlags.USE_ALPHA_BLEND));
        Shaders.Register(ResourceIndex.Shaders.Particle, ShaderLoader.LoadSurfaceShader("Particle", CommonBehaviourFlags.TRANSFORM_TO_NDC | CommonBehaviourFlags.USE_ALPHA_BLEND | CommonBehaviourFlags.USE_CAMERA_POS));
    }

    static void LoadMeshes()
    {
        var meshDir = Path.Combine(rootPath, "meshes");
        if (!Directory.Exists(meshDir)) return;

        foreach (var file in Directory.GetFiles(meshDir, "*.obj"))
        {
            var name = Path.GetFileName(file);
            Meshes.Register(name, MeshLoader.LoadFromObj(name));
        }
    }

    static void LoadTextures()
    {
        var texDir = Path.Combine(rootPath, "textures");
        if (!Directory.Exists(texDir)) return;

        foreach (var file in Directory.GetFiles(texDir, "*.*"))
        {
            var ext = Path.GetExtension(file).ToLower();
            if (ext == ".png" || ext == ".bmp" || ext == ".jpg")
            {
                var name = Path.GetFileName(file);
                Textures.Register(name, TextureLoader.LoadTexture(Path.GetFileName(file)));
            }
        }
    }

    static void LoadModels()
    {
        var modelDir = Path.Combine(rootPath, "models");
        if (!Directory.Exists(modelDir)) return;

        foreach (var file in Directory.GetFiles(modelDir, "*.t3m.yaml"))
        {
            var name = Path.GetFileName(file).Replace(".t3m.yaml", "");
            Models.Register(name, ModelParser.ParseModelDefinition(name));
        }
    }

    static void LoadAudio()
    {
        var audioDir = Path.Combine(rootPath, "audio");
        if (!Directory.Exists(audioDir)) return;

        foreach (var file in Directory.GetFiles(audioDir, "*.wav"))
        {
            var name = Path.GetFileName(file);
        }
    }

    private static class ShaderLoader
    {
        const string shaderPath = rootPath + "shaders/";

        /// <summary>
        /// Loads a surface shader. Specify a name and both <code>.vert.glsl   .frag.glsl</code> are appended automatically
        /// </summary>
        /// <param name="name">The name of the shader in the FS</param>
        /// <param name="flags"></param>
        /// <returns>The surface shader, ready to use</returns>
        public static SurfaceShader LoadSurfaceShader(string name, CommonBehaviourFlags flags)
        {
            var v = ReadShader("surface", $"{name}.vert");
            var f = ReadShader("surface", $"{name}.frag");
            return new(v, f, flags);
        }

        static Shader LoadShader(string path, string name, ShaderType shaderType)
        {
            var shaderContent = ReadShader(path, name);
            Shader shader = new(shaderType, shaderContent);
            return shader;
        }

        static string ReadShader(string path, string name)
        {
            var filePath = Path.Combine(shaderPath, path, name + ".glsl");
            return File.ReadAllText(filePath);
        }
    }

    private static class MeshLoader
    {
        const string modelPath = rootPath + "meshes/";
        /// <summary>
        /// Loads a mesh from a .obj file
        /// </summary>
        /// <param name="path">The path to the model</param>
        /// <returns>The Mesh</returns>
        public static Mesh LoadFromObj(string path)
        {
            var obj = ObjLoader.Load(Path.Combine(modelPath, path));
            return new(obj.Item1, obj.Item2);
        }
    }

    private static class TextureLoader
    {
        const string texturePath = rootPath + "textures/";
        /// <summary>
        /// Loads a texture
        /// </summary>
        /// <param name="path">The path to the image file</param>
        /// <returns>The texture</returns>
        public static Texture LoadTexture(string path)
        {
            var img = new Texture(Path.Combine(texturePath, path));
            return img;
        }
    }

    private static class ModelParser
    {
        static IDeserializer deserialiser = new DeserializerBuilder().Build();
        const string texturePath = rootPath + "models/";
        public static ModelDefinition ParseModelDefinition(string path)
        {
            var content = File.ReadAllText(Path.Combine(texturePath, $"{path}.t3m.yaml"));
            return deserialiser.Deserialize<ModelDefinition>(content);
        }
    }
}





