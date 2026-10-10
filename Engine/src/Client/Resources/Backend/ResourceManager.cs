using System.Diagnostics;
using Terr3D.Utils;

namespace Terr3D.Client.Resources;

/// <summary>
/// Responsible for loading GPU assets (shaders, meshes and textures) and providing static access to them
/// </summary>
internal class ResourceManager
{
    private readonly string rootDirectory;
    private readonly Dictionary<string, ResourceEntry> entries = [];
    private readonly Dictionary<string, IImporter> importers = [];
    private readonly Queue<ResourceEntry> toCollect = new();
    private readonly Lock gate = new();


    internal ResourceManager(string rootDirectory)
    {
        this.rootDirectory = rootDirectory;
    }


    public void RegisterImporter<T>(Importer<T> importer, params string[] extensions) where T : Resource
    {
        foreach (var ext in extensions)
            importers[ext.ToLowerInvariant()] = importer;
    }

    public void ScanDirectory()
    {
        if (!Directory.Exists(rootDirectory)) return;

        foreach (var file in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories))
        {
            var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
            if (!importers.TryGetValue(ext, out var importer)) continue;

            var path = System.IO.Path.GetRelativePath(rootDirectory, file).Replace('\\', '/');
            entries[path] = new ResourceEntry
            {
                Path = path,
                Type = importer.ResourceType,
                State = ResourceState.Unloaded
            };
        }
    }


    public static string GetActualPath(string path) => path; //TODO: impl

    public ResourceRef<T> Register<T>(string path, Func<T> factory, bool pinned = false) where T : Resource
    {
        lock (gate)
        {
            if (entries.ContainsKey(path))
                throw new InvalidOperationException($"Resource already registered: {path}");

            var entry = new ResourceEntry
            {
                Path = path,
                Type = typeof(T),
                State = ResourceState.Unloaded,
                Factory = factory,
                Pinned = pinned
            };
            entries[path] = entry;
            return new ResourceRef<T>(entry);
        }
    }

    public bool Unregister(string path)
    {
        lock (gate)
        {
            if (!entries.TryGetValue(path, out var e) || e.RefCount > 0) return false;
            if (e.Instance != null) Unload(e);
            entries.Remove(path);
            return true;
        }
    }

    public bool TryGet<T>(string path, out ResourceRef<T> result) where T : Resource
    {
        lock (gate)
        {
            if (!entries.TryGetValue(path, out var entry))
            {
                result = default;
                return false;
            }
            if (!typeof(T).IsAssignableFrom(entry.Type))
                throw new InvalidCastException($"{path} is a {entry.Type.Name}, not a {typeof(T).Name}");

            result = new ResourceRef<T>(entry);
            return true;
        }
    }

    public ResourceRef<T> Get<T>(string path) where T : Resource
    {
        if (TryGet<T>(path, out var r)) return r;
        throw new KeyNotFoundException($"Resource not registered: {path}");
    }


    #region Ref counting

    internal void Acquire(ResourceEntry e)
    {
        lock (gate)
        {
            e.RefCount++;
            if (e.State == ResourceState.Unloaded || e.State == ResourceState.Errored)
            {
                try { Load(e); }
                catch { e.RefCount--; throw; }
            }
        }
    }

    internal void Release(ResourceEntry e)
    {
        lock (gate)
        {
            if (e.RefCount <= 0)
            {
                var msg = $"Resource at {e.Path} was released more times than acquired!";
                Diagnostics.Error(msg);
                Debug.Fail(msg);
                return;
            }
            if (--e.RefCount == 0 && !e.Pinned)
                toCollect.Enqueue(e);
        }
    }


    public void CollectGarbage()
    {
        lock (gate)
        {
            while (toCollect.TryDequeue(out var e))
            {
                if (e.RefCount == 0 && e.State == ResourceState.InMemory)
                    Unload(e);
            }
        }
    }
    #endregion
    #region Instance Access

    internal Resource GetInstance(ResourceEntry e)
    {
        lock (gate)
        {
            return e.Instance ?? throw new InvalidOperationException($"Resource is not loaded: {e.Path}");
        }
    }



    private void Load(ResourceEntry e)
    {
        e.State = ResourceState.LoadInProgress;
        try
        {
            Resource instance;
            if (e.Factory != null)
            {
                instance = e.Factory();
            }
            else
            {
                var ext = System.IO.Path.GetExtension(e.Path).ToLowerInvariant();
                if (!importers.TryGetValue(ext, out var importer))
                    throw new InvalidOperationException($"No importer for '{ext}' ({e.Path})");

                using var stream = File.OpenRead(System.IO.Path.Combine(rootDirectory, e.Path));
                instance = importer.Import(stream, e.Path);
            }

            instance.Init(e.Path);
            e.Instance = instance;
            e.State = ResourceState.InMemory;
        }
        catch
        {
            e.State = ResourceState.Errored;
            throw;
        }
    }

    private void Unload(ResourceEntry e)
    {
        e.Instance!.Collect();
        e.Instance = null;
        e.State = ResourceState.Unloaded;
    }



    public void Dispose()
    {
        lock (gate)
        {
            foreach (var e in entries.Values)
            {
                if (e.RefCount > 0)
                    Console.Error.WriteLine($"Still acquired at shutdown: {e.Path} (count {e.RefCount})");

                if (e.Instance != null) Unload(e);
            }
            entries.Clear();
            toCollect.Clear();
        }
    }


    #endregion

}


#if false
    const string rootPath = "res/";

    public static RegistryOfResources<SurfaceShader> Shaders { get; } = new();
    public static RegistryOfResources<Mesh> Meshes { get; } = new();
    public static RegistryOfResources<Texture> Textures { get; } = new();
    public static RegistryOfResources<ModelDefinition> Models { get; } = new();
    public static RegistryOfResources<int> Audio { get; } = new();


    private readonly Dictionary<(Type, string ext), IImporter> importers = new();

    public void RegisterImporter<T>(Importer<T> importer, params string[] extensions) where T : Resource
    {
        foreach (var ext in extensions)
            importers[(typeof(T), ext)] = importer;
    }

    private Resource LoadInstance(ResourceEntry entry)
    {
        var ext = Path.GetExtension(entry.Path);
        if (!importers.TryGetValue((entry.Type, ext), out var importer))
            throw new InvalidOperationException($"No importer for {entry.Type.Name} with '{ext}' ({entry.Path})");

        return importer.Load(entry);
    }


    /// <summary>
    /// Load all resources
    /// </summary>
    public static void InitialiseResources()
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
        Shaders.Register(ResourceIndex.Shaders.Volume, ShaderLoader.LoadSurfaceShader("Volume", CommonBehaviourFlags.TRANSFORM_TO_NDC | CommonBehaviourFlags.USE_CAMERA_POS | CommonBehaviourFlags.USE_ALPHA_BLEND));
        Shaders.Register(ResourceIndex.Shaders.FlatColor, ShaderLoader.LoadSurfaceShader("FlatColor", CommonBehaviourFlags.USE_WIREFRAME | CommonBehaviourFlags.TRANSFORM_TO_NDC));
        Shaders.Register(ResourceIndex.Shaders.Depth, ShaderLoader.LoadSurfaceShader("Depth", CommonBehaviourFlags.USE_MODEL_MATRIX));
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


#endif