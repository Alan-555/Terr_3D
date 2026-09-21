using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.Entities;
using Terr3D.Server.World;
using Terr3D.Utils;

namespace Terr3D.Server.WorldGen;

/// <summary>
/// Class responsible for generating the world
/// </summary>
public class WorldGenerator
{

    public const int QuadsPerChunk = 26; //how may quads (per axis) do we use? This is arbitrary and good ballance has to be made. More chunks = more work for the CPU to cull them, less chunks = CPU can't cull so GPU will have to render a lot of triangles
    public const int QuadsPerMeter = 2; //from the assignment. Sampling each 0.5meters means we have two quads per meter

    /// <summary>
    /// Generates all terrain meshes and packs chunk heights + the world pos into one tuple FIXME: not ideal
    /// </summary>
    /// <param name="worldSpawn">The worldspawn to generate for</param>
    public static List<(Mesh, Vector2, Vector2)> GenerateTerrainMesh(Worldspawn worldSpawn)
    {
        //calculate the number of total chunks (per axis)
        var NumChunks = (int)Math.Ceiling(worldSpawn.TerrainSize * QuadsPerMeter / QuadsPerChunk);
        NumChunks = (int)MathF.Sqrt(WorldGeneratorHelpers.FindPowerOfFour(NumChunks * NumChunks));

        //the step we take
        float step = worldSpawn.TerrainSize / NumChunks;

        //the root offset of the origin, so the world is centred
        var rootOffset = worldSpawn.Pivot;

        List<(Mesh, Vector2, Vector2)> meshes = [];
        for (int x = 0; x < NumChunks; x++)
        {
            for (int z = 0; z < NumChunks; z++)
            {
                //the world pos
                float px = x * step + rootOffset.X;
                float pz = z * step + rootOffset.Z;
                float h = 0;//(float)Random.Shared.NextDouble();

                var (terrainMesh, heightData) = WorldGeneratorHelpers.GeneratePlane(
                    QuadsPerChunk,
                    step,
                    (x, z) => worldSpawn.SampleHeightSlow(new Vector2(x, z)) + h,
                    (x, z) => worldSpawn.GetTerrainNormal((x, 0, z), 0.5f),
                    new(px, 0, pz)
                );


                meshes.Add((terrainMesh, (px, pz), heightData));
            }
        }

        return meshes;
    }

    static ModelEntity SpawnModelEntity(Worldspawn worldspawn, ModelDefinition model, Vector2 pos)
    {

        var modelEntity = new ModelEntity(worldspawn.Onstage, model);

        var worldSpace2 = worldspawn.NormalisedMapSpaceToWorld(pos);
        var worldSpace3 = new Vector3(worldSpace2.X, worldspawn.SampleHeightSlow(worldSpace2), worldSpace2.Y);
        var terrainEntity = worldspawn.GetTerrainEntityAt(worldSpace3.X, worldSpace3.Z);
        modelEntity.Transform.Position = worldSpace3;
        modelEntity.Transform.SetParent(terrainEntity.Transform);
        modelEntity.Transform.LocalEuler = new(0, Random.Shared.NextSingle() * MathF.Tau, 0);
        modelEntity.ComputeStaticBB();
        return modelEntity;
    }


}



public static class WorldGeneratorHelpers
{

    public static int FindPowerOfFour(int n)
    {
        double exponent = Math.Log(n, 4);

        int lowerExp = (int)Math.Floor(exponent);
        int upperExp = (int)Math.Ceiling(exponent);

        int lowerPower = (int)Math.Pow(4, lowerExp);
        int upperPower = (int)Math.Pow(4, upperExp);

        if (Math.Abs(n - lowerPower) <= Math.Abs(n - upperPower))
        {
            return lowerPower;
        }
        else
        {
            return upperPower;
        }
    }

    /// <summary>
    /// Generates a bumpy terrain, like a source displacement, just that each vertex is on a horizontal grid
    /// </summary>
    /// <param name="subdivisions">The number of subdivisions per axis</param>
    /// <param name="size">The world space size</param>
    /// <param name="GetHeight">A delegate that returns the terrain height for a given x and z</param>
    /// <param name="GetNormal">A delegate that returns the terrain normal for a given x and z</param>
    /// <param name="origin">The global offset</param>
    /// <returns>A mesh and a min max height vector</returns>
    public static (Mesh, Vector2) GeneratePlane(int subdivisions, float size, Func<float, float, float> GetHeight, Func<float, float, Vector3> GetNormal, Vector3 origin)
    {
        float minHeight = int.MaxValue, maxHeight = int.MinValue;
        //ensure we have at least one quad
        int segments = Math.Max(1, subdivisions);

        //calculate the number of required vertices and triangles in one axis
        int vertexCount = (segments + 1) * (segments + 1);
        int triangleCount = segments * segments * 2;

        Vertex[] vertices = new Vertex[vertexCount];
        Triangle[] triangles = new Triangle[triangleCount];

        float step = size / segments;

        //generate Vertices
        int vIndex = 0;
        for (int z = 0; z <= segments; z++)
        {
            for (int x = 0; x <= segments; x++)
            {
                //position in the model space
                float px_m = x * step;
                float pz_m = z * step;
                //position in world space
                float px = px_m + origin.X;
                float pz = pz_m + origin.Z;

                //retrieve the normal and the height
                var normal = GetNormal(px, pz);
                var height = GetHeight(px, pz);

                //update height
                if (height > maxHeight)
                    maxHeight = height;
                else if (height < minHeight)
                    minHeight = height;

                //push new vertex
                vertices[vIndex++] = new Vertex(new Vector3(px_m, height, pz_m), normal, (0, 0));
            }
        }

        //generate triangle indices
        int tIndex = 0;
        for (int z = 0; z < segments; z++)
        {
            for (int x = 0; x < segments; x++)
            {
                //calculate the indexes for the quad
                uint topLeft = (uint)(z * (segments + 1) + x);
                uint topRight = topLeft + 1;
                uint bottomLeft = (uint)((z + 1) * (segments + 1) + x);
                uint bottomRight = bottomLeft + 1;

                //first triangle
                triangles[tIndex++] = new Triangle
                {
                    i0 = topLeft,
                    i1 = bottomLeft,
                    i2 = topRight
                };

                //second triangle
                triangles[tIndex++] = new Triangle
                {
                    i0 = topRight,
                    i1 = bottomLeft,
                    i2 = bottomRight
                };
            }
        }

        return (new Mesh(vertices, triangles), (minHeight, maxHeight));
    }
}