using System.Security.Cryptography.X509Certificates;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.World;
using Terr3D.Server.WorldGen;
using Terr3D.Utils;
using YamlDotNet.Core.Events;

namespace Terr3D.Server.Entities;

/// <summary>
/// This entity is crucial for the correct runtime. It manages common features of the engine, like the terrain, skybox and other very important things. 
/// The engine will not function without this entity
/// </summary>
public class Terrain : Entity
{
    public float TerrainSize { get; private set; }

    public Vector3 Pivot => new(-TerrainSize / 2f, 0, -TerrainSize / 2f);

    List<TerrainEntity> terrainEntities = [];
    public List<TerrainEntity> TerrainEntities => terrainEntities;

    List<ModelEntity> modelEntities = [];
    public List<ModelEntity> ModelEntities => modelEntities;

    HeighMap _heightMap;


    public Terrain(string name, Entity parent, float worldSpaceSize, int heightMapSize) : base(name, parent, true)
    {
        //Store terrain size in units (meters)
        TerrainSize = worldSpaceSize;
        _heightMap = new(heightMapSize, heightMapSize);
        Diagnostics.Info("Begin terrain generation...");
        GenerateChunks();

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
            var ent = new TerrainEntity($"TerrainChunk{pos}", this, heightData);
            ent.Transform.Position = new(pos.X, 0, pos.Y);
            var r = ent.AddComponent(new Renderer(ResourceManager.Shaders[ResourceIndex.Shaders.Shaded], mesh, RendererClass.RENDERER_STATIC));
            r.material = mat;
            terrainEntities.Add(ent);
        }
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
    public Vector2 WorldToHeightMapSpace(Vector3 pos)
    {
        var terrainSpace = pos - Pivot;
        return terrainSpace.Xz / TerrainSize;
    }

    /// <summary>
    /// Converts a Vector2 normalised map space to world position
    /// </summary>
    /// <param name="pos">The NMS position</param>
    /// <returns>The world space position</returns>
    public Vector2 HeightMapSpaceToWorld(Vector2 pos)
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
        var mapSpacePos = WorldToHeightMapSpace(worldPos);
        float height = _heightMap.Sample(mapSpacePos);

        return height;
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


    public Vector3 GetTerrainNormal(Vector3 position, float delta = 0.1f)
    {
        float left = SampleHeight(position + new Vector3(-delta, 0, 0));
        float right = SampleHeight(position + new Vector3(delta, 0, 0));
        float back = SampleHeight(position + new Vector3(0, 0, -delta));
        float forward = SampleHeight(position + new Vector3(0, 0, delta));

        Vector3 normal = new Vector3(left - right, 2f * delta, back - forward);

        return normal.Normalized();
    }
}


class HeighMap
{
    public int SizeX {get; private init;}
    public int SizeZ {get; private init;}
    private readonly float[] map;

    public HeighMap(int sizeX, int sizeZ)
    {
        SizeX = sizeX;
        SizeZ = sizeZ;
        map = new float[sizeX * sizeZ];
    }

    public float GetActualHeightAt(int x, int z)=> map[x + z * SizeX];

    public float Sample(Vector3 pos) => Sample(pos.X, pos.Z);
    public float Sample(Vector2 pos) => Sample(pos.X, pos.Y);

    /// <summary>
    /// Samples terrain in HeighMap space using bi-linear interpolation
    /// </summary>
    /// <returns>The interpolated height at that point</returns>
    public float Sample(float x, float z)
    {
        int x1 = (int)Math.Floor(x);
        int z1 = (int)Math.Floor(z);

        int x2 = Math.Min(x1 + 1, SizeX);
        int z2 = Math.Min(z1 + 1, SizeZ);

        float fx = x - x1;
        float fy = z - z1;

        float r00 = GetActualHeightAt(x1, z1); // Top-Left
        float r10 = GetActualHeightAt(x2, z1); // Top-Right
        float r01 = GetActualHeightAt(x1, z2); // Bottom-Left
        float r11 = GetActualHeightAt(x2, z2); // Bottom-Right

        float inverseFx = 1.0f - fx;
        float inverseFy = 1.0f - fy;

        float w00 = inverseFx * inverseFy;
        float w10 = fx * inverseFy;
        float w01 = inverseFx * fy;
        float w11 = fx * fy;

        float blendedHeight = r00 * w00 + r10 * w10 + r01 * w01 + r11 * w11;

        return blendedHeight;
    }
}