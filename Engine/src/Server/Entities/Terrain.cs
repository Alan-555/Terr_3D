using System.Security.Cryptography.X509Certificates;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.Engine;
using Terr3D.Server.Shared;
using Terr3D.Server.WorldGen;
using Terr3D.Utils;
using YamlDotNet.Core.Events;

namespace Terr3D.Server.Entities;

/// <summary>
/// This entity is crucial for the correct runtime. It manages common features of the engine, like the terrain, skybox and other very important things. 
/// The engine will not function without this entity
/// </summary>
public class Terrain : Entity, IGroundProvider
{


    public const int QuadsPerChunk = 30; //how may quads (per axis) do we use? This is arbitrary and good ballance has to be made. More chunks = more work for the CPU to cull them, less chunks = CPU can't cull so GPU will have to render a lot of triangles
    public const int QuadsPerMeter = 2; //from the assignment. Sampling each 0.5meters means we have two quads per meter

    /// <summary>
    /// The region size per axis (since regions are square)
    /// </summary>
    public float RegionSize { get; private init; }

    /// <summary>
    /// Height map res
    /// </summary>
    public int HeightMapResolution { get; private init; }

    /// <summary>
    /// Number of chunks in a region per one axis
    /// </summary>
    public int RegionNumChunks { get; private init; }

    private readonly Dictionary<(int x, int z), HeightMap> _regions = [];

    private int maxRegionX = int.MinValue, minRegionX = int.MaxValue;
    private int maxRegionZ = int.MinValue, minRegionZ = int.MaxValue;


    public Terrain(string name, Entity parent, float regionWorldSize, int heightMapResolution, Vector3 pos = new()) : base(name, parent, true)
    {
        Transform.Position = pos;

        RegionSize = regionWorldSize;

        HeightMapResolution = heightMapResolution;

        //calculate the number of total chunks (per axis)
        var targetNumChunks = (int)Math.Ceiling(RegionSize * QuadsPerMeter / QuadsPerChunk);
        RegionNumChunks = (int)MathF.Sqrt(WorldGeneratorHelpers.FindPowerOfFour(targetNumChunks * targetNumChunks));

        AddComponent(new Ground(this));

    }

    public void Build()
    {
        foreach (((int x, int z), _) in _regions)
        {
            GenerateRegion(x, z);
        }
    }

    public void AddRegion(int regionX, int regionZ, HeightMap heighMap)
    {
        if (!_regions.TryAdd((regionX, regionZ), heighMap))
        {
            throw new InvalidOperationException($"Region ({regionX}, {regionZ}) is already present");
        }

        if (regionX < minRegionX) minRegionX = regionX;
        if (regionZ < minRegionZ) minRegionZ = regionZ;

        if (regionX > maxRegionX) maxRegionX = regionX;
        if (regionZ > maxRegionZ) maxRegionZ = regionZ;
    }


    void GenerateRegion(int regionX, int regionZ)
    {
        Vector3 offset = new(regionX * RegionSize, 0f, regionZ * RegionSize);

        var chunks = WorldGenerator.GenerateRegionMeshes(this, offset);
        var mat = new ShadedMaterial()
        {
            diffuse = new Vector3(43, 115, 33) / 255f,
            specular = new Vector3(17, 46, 13) / 255f,
            shininess = 5f
        };

        //spawn a terrain entity for each chunk
        foreach (var (mesh, worldPos) in chunks)
        {
            var ent =
            new EmptyEntity($"TerrainChunk{worldPos}", this, true)
            .WithComponent(new Renderer(ResourceManager.Shaders[ResourceIndex.Shaders.Shaded], mesh, RendererClass.RENDERER_STATIC)
            {
                material = mat
            });
            ent.Transform.Position = new Vector3(worldPos.X, RandHelper.RandFloatNormalised(), worldPos.Y);
        }
    }

    /// <summary>
    /// Gets the bounds of the entire terrain (all regions combined)
    /// </summary>
    /// <returns></returns>
    public Bounds GetTerrainBounds()
    {
        var origin = Transform.Position.Xz + new Vector2(RegionSize) / 2f;
        return new(new(origin.X, 0f, origin.Y), new(RegionSize / 2f, 0f, RegionSize / 2f));
    }

    public int GetTotalNumChunks() => RegionNumChunks * RegionNumChunks;

    /// <summary>
    /// Converts a Vector3 world position to normalised map space
    /// </summary>
    /// <param name="pos">The world position</param>
    /// <returns>The position in the normalised map space coordinates</returns>
    public Vector2 WorldToHeightMapSpace(Vector3 pos)
    {
        var terrainSpace = pos - Transform.Position;
        return terrainSpace.Xz / RegionSize;
    }

    /// <summary>
    /// Converts a Vector2 normalised map space to world position
    /// </summary>
    /// <param name="pos">The NMS position</param>
    /// <returns>The world space position</returns>
    public Vector2 HeightMapSpaceToWorld(Vector2 pos)
    {
        var mapSpace = pos * RegionSize;
        return mapSpace + Transform.Position.Xz;
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


    public Vector3 GetTerrainNormal(Vector2 position, float delta = 0.1f)
    {
        float left = SampleHeight(position + new Vector2(-delta, 0));
        float right = SampleHeight(position + new Vector2(delta, 0));
        float back = SampleHeight(position + new Vector2(0, -delta));
        float forward = SampleHeight(position + new Vector2(0, delta));

        Vector3 normal = new Vector3(left - right, 2f * delta, back - forward);

        return normal.Normalized();
    }

    public float GetHeightAt(float x, float z) => SampleHeight(new Vector2(x, z));

    public Vector3 GetNormalAt(float x, float z) => GetTerrainNormal(new Vector2(x, z));

    public bool Contains(float x, float z) => MathF.Abs(x - Transform.Position.X) <= RegionSize / 2f && MathF.Abs(z - Transform.Position.Z) <= RegionSize / 2f;

    public Vector2 GetOrigin() => Transform.Position.Xz + new Vector2(RegionSize) / 2f;
}



public class HeightMap
{
    public int Resolution { get; private init; }
    public float[] map;

    public HeightMap(int resolution)
    {
        Resolution = resolution;
        map = new float[Resolution * Resolution];
    }

    private Vector2 FromNormalised(float x, float z) => new(float.Clamp(x, 0f, 1f) * (Resolution - 1), float.Clamp(z, 0f, 1f) * (Resolution - 1));

    private Vector2 ToNormalised(int x, int z) => new(x / (Resolution + 1f), z / (Resolution + 1f));

    public float GetActualHeightAt(int x, int z) => map[x + z * Resolution];
    public float SetHeightAt(int x, int z, float height) => map[x + z * Resolution] = height;

    public float Sample(Vector3 pos) => Sample(pos.X, pos.Z);
    public float Sample(Vector2 pos) => Sample(pos.X, pos.Y);

    /// <summary>
    /// Samples terrain in HeighMap space using bi-linear interpolation
    /// </summary>
    /// <returns>The interpolated height at that point</returns>
    public float Sample(float x, float z)
    {
        FromNormalised(x, z).Deconstruct(out x, out z);
        int x1 = (int)Math.Floor(x);
        int z1 = (int)Math.Floor(z);

        int x2 = Math.Min(x1 + 1, Resolution - 1);
        int z2 = Math.Min(z1 + 1, Resolution - 1);

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


    public void Noise()
    {
        List<(Vector2 pos, float height)> points = [];
        for (int i = 0; i < 10; i++)
        {
            points.Add((new Vector2(RandHelper.RandFloatNormalised(), RandHelper.RandFloatNormalised()), RandHelper.RandFloat(0.01f, 64f)));
        }

        for (int x = 0; x < Resolution; x++)
        {
            for (int z = 0; z < Resolution; z++)
            {
                var pos = ToNormalised(x, z);
                float sumHeight = 0f;
                float sumWeight = 0f; // Track total weight for proper interpolation

                foreach (var point in points)
                {
                    var dist = Vector2.Distance(pos, point.pos);

                    // Clamp distance to prevent division by zero (extreme spikes)
                    dist = dist < 0.01f ? 0.01f : dist;

                    float weight = 1f / (dist * dist);
                    sumHeight += point.height * weight;
                    sumWeight += weight;
                }

                // Final height is the weighted average, not divided by point count
                SetHeightAt(x, z, sumHeight / sumWeight);
            }
        }
    }
}