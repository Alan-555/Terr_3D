using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.World;
using Terr3D.Server.WorldGen;
using Terr3D.Utils;

namespace Terr3D.Server.Entities;

/// <summary>
/// This entity is crucial for the correct runtime. It manages common features of the engine, like the terrain, skybox and other very important things. 
/// The engine will not function without this entity
/// </summary>
public class Worldspawn : Entity, IWorldspawnProvider
{
    //How is the terrain map transformed to world coordinates
    public const float HorizontalPixelUnits = 0.5f;
    public const float VerticalPixelUnits = 0.05f;

    public float TerrainHeight => VerticalPixelUnits * 255;
    public float TerrainSize { get; private set; }

    public Vector3 Pivot => new(-TerrainSize / 2f, 0, -TerrainSize / 2f);

    Image<Rgba32> terrainMap;

    List<TerrainEntity> terrainEntities = [];
    public List<TerrainEntity> TerrainEntities => terrainEntities;

    List<ModelEntity> modelEntities = [];
    public List<ModelEntity> ModelEntities => modelEntities;

    public BoidManager BoidManager => _boidManager;
    private readonly BoidManager _boidManager;

    Worldspawn IWorldspawnProvider.Worldspawn => this;

    public Camera CurrentCamera { get => _currentCamera; set => _currentCamera = value; }
    Camera _currentCamera;

    public bool debugCulling = false;

    public Worldspawn(Scene scene, string name, string terrainMapPath) : base(scene, name, true)
    {
        Register();
        terrainMap = Image.Load<Rgba32>(terrainMapPath);

        if (terrainMap.Width != terrainMap.Height)
        {
            throw new Exception("Terrain map has to be a square!");
        }

        //Store terrain size in units (meters)
        TerrainSize = terrainMap.Width * HorizontalPixelUnits;
        Diagnostics.Info("Begin terrain generation...");
        GenerateChunks();
        PopulateTerrain();



        _boidManager = new(scene, "BoidManagerSingleton");

        _ = new Environment(Onstage, "env_weather", false);

    }

    public void Register()
    {
        Onstage.Globals.Register<IWorldspawnProvider>(this);
    }

    void GenerateChunks()
    {
        var chunks = WorldGenerator.GenerateTerrainMesh(this);
        var mat = new ShadedMaterial()
        {
            diffuse = new Vector3(43, 115, 33) / 255f,
            specular = new Vector3(17, 46, 13) / 255f,
            shininess = 5f
        };

        //spawn a terrain entity for each chunk
        foreach (var (mesh, pos, heightData) in chunks)
        {
            var ent = new TerrainEntity(Onstage, $"TerrainChunk{pos}", heightData);
            ent.Transform.Position = new(pos.X, 0, pos.Y);
            var r = ent.AddComponent(new Renderer(ResourceManager.Shaders[ResourceIndex.Shaders.Shaded], mesh, RendererClass.RENDERER_STATIC));
            r.material = mat;
            terrainEntities.Add(ent);
            ent.Transform.SetParent(Transform);
        }
    }

    void PopulateTerrain()
    {
        WorldGenerator.PopulateWithModels(this, modelEntities, terrainMap);
    }




    public override void OnUpdate(float dt)
    {
        //rotate the sun
        

    }

    /// <summary>
    /// Returns the TerrainEntity at the specified world space coordinates
    /// </summary>
    /// <param name="x">The x coordinate</param>
    /// <param name="z">The z coordinate</param>
    /// <returns>The terrain entity at that pos</returns>
    public TerrainEntity GetTerrainEntityAt(float x, float z)
    {
        int sqr = (int)MathF.Sqrt(TerrainEntities.Count);
        float chunkSize = TerrainSize / sqr;

        int e_x = (int)((x + TerrainSize / 2f) / chunkSize);
        int e_z = (int)((z + TerrainSize / 2f) / chunkSize);

        return TerrainEntities[e_x * sqr + e_z];
    }


    /// <summary>
    /// Converts a Vector3 world position to normalised map space
    /// </summary>
    /// <param name="pos">The world position</param>
    /// <returns>The position in the normalised map space coordinates</returns>
    public Vector2 WorldToNormalisedMapSpace(Vector3 pos)
    {
        var terrainSpace = pos - Pivot;
        return terrainSpace.Xz / TerrainSize;
    }

    /// <summary>
    /// Converts a Vector2 normalised map space to world position
    /// </summary>
    /// <param name="pos">The NMS position</param>
    /// <returns>The world space position</returns>
    public Vector2 NormalisedMapSpaceToWorld(Vector2 pos)
    {
        var mapSpace = pos * TerrainSize;
        return mapSpace + Pivot.Xz;
    }

    /// <summary>
    /// Samples height at the specified Vector3 world position
    /// </summary>
    /// <param name="worldPos">The position in world</param>
    /// <returns>World space height at that position</returns>
    public float SampleHeight(Vector3 worldPos)
    {
        var mapSpacePos = WorldToNormalisedMapSpace(worldPos) * (terrainMap.Width - 1);
        float height = WorldHelpers.SampleBilinearHeight(terrainMap, mapSpacePos.X, mapSpacePos.Y);

        return height * TerrainHeight;
    }

    /// <summary>
    /// Samples height at the specified X,Z world position (ignoring Y axis)
    /// </summary>
    /// <param name="worldPos">The position in the world (X,Z)</param>
    /// <returns>World space height at that position</returns>
    public float SampleHeight(Vector2 worldPos)
    {
        return SampleHeight(new Vector3(worldPos.X, 0, worldPos.Y));
    }

    /// <summary>
    /// Samples height at the specified Vector3 world position
    /// USES GAUSSIAN BLUR! VERY SLOW!
    /// </summary>
    /// <param name="worldPos">The position in world</param>
    /// <returns>World space height at that position</returns>
    public float SampleHeightSlow(Vector3 worldPos)
    {
        var mapSpacePos = WorldToNormalisedMapSpace(worldPos) * (terrainMap.Width - 1);
        float height = WorldHelpers.SampleGaussianHeight(terrainMap, mapSpacePos.X, mapSpacePos.Y);

        return height * TerrainHeight;
    }

    /// <summary>
    /// Samples height at the specified X,Z world position (ignoring Y axis)
    /// USES GAUSSIAN BLUR! VERY SLOW!
    /// </summary>
    /// <param name="worldPos">The position in the world (X,Z)</param>
    /// <returns>World space height at that position</returns>
    public float SampleHeightSlow(Vector2 worldPos)
    {
        return SampleHeightSlow(new Vector3(worldPos.X, 0, worldPos.Y));
    }

    public Vector3 GetTerrainNormal(Vector3 position, float delta = 0.1f)
    {
        float left = SampleHeight(position + new Vector3(-delta, 0, 0));
        float right = SampleHeight(position + new Vector3(delta, 0, 0));
        float back = SampleHeight(position + new Vector3(0, 0, -delta));
        float forward = SampleHeight(position + new Vector3(0, 0, delta));

        Vector3 normal = new Vector3(left - right, 2f * delta, back - forward);

        return normal.Normalized();
    }

    public float GetHeight(Vector3 worldPosition)
    {
        throw new NotImplementedException();
    }

    public Vector3 GetNormal(Vector3 worldPosition)
    {
        throw new NotImplementedException();
    }
}