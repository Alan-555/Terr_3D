using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using Terr3D.Server.Shared;
using Terr3D.Server.Components;
using Terr3D.Server.Entities;

namespace Terr3D.Server.World;


public static class FrustumCulling
{
    static Vector3 _chunkHalfExtents; //TODO: investigate static's impacting on other scenes

    public static QuadTreeNode ConstructForTerrain(Terrain worldSpawn)
    {
        return new(new());
        int numChunks = worldSpawn.TerrainEntities.Count;

        _chunkHalfExtents = new Vector3(worldSpawn.TerrainSize / MathF.Sqrt(numChunks)) * 0.5f;
        _chunkHalfExtents.Y = 0;

        int depth = (int)Math.Log(numChunks, 4); //assuming numChunks is divisible by four
        var halfSize = worldSpawn.TerrainSize / 2;
        var rootVolume = new Volume_AABB(Vector3.Zero, new(halfSize));
        return ConstructForTerrain(worldSpawn, rootVolume, depth);
    }

    private static QuadTreeNode ConstructForTerrain(Terrain worldSpawn, Volume_AABB volume, int depth)
    {
        if (depth <= 0)
        {
            //this is a leaf node. Let's find the terrain chunk and other objects here
            var chunk = worldSpawn.GetTerrainEntityAt(volume.Position.X, volume.Position.Z);
            float minHeight = chunk.minHeight;
            float maxHeight = chunk.maxHeight;

            //now we find all intersecting models
            List<Renderer> models = [];
            foreach (var model in worldSpawn.ModelEntities)
            {
                if (model.Renderer.staticBoundingBox.IntersectsInfHeight(volume))
                {
                    //we found one. Also, update the max/min heights
                    models.Add(model.Renderer);
                    if (model.Renderer.staticBoundingBox.MinPoint.Y < minHeight)
                        minHeight = model.Renderer.staticBoundingBox.MinPoint.Y;
                    if (model.Renderer.staticBoundingBox.MaxPoint.Y > maxHeight)
                        maxHeight = model.Renderer.staticBoundingBox.MaxPoint.Y;
                }
            }

            List<Collider> colliders = [];
            //then we find all static colliders
            foreach (var collider in worldSpawn.Onstage.SceneRegistry.GetComponents<Collider>())
            {
                //if this collider is not static, we won't put it in the leaf
                if (!collider.IsStatic) continue;

                //check if this collider is inside the volume (ignoring Y-component)
                if (collider.IntersectsInfHeight(volume))
                {
                    colliders.Add(collider);
                }
            }

            //calculate the height data
            var halfHeight = (maxHeight - minHeight) / 2f;
            float y = minHeight + halfHeight;


            volume.Position.Y = y;
            volume.HalfExtents.Y = halfHeight;

            //return the leaf
            return new(volume, [chunk.Components.OfType<Renderer>().First(), .. models], [.. colliders]);
            //throw new Exception($"No chunk found for volume {volume}");
        }

        var node = new QuadTreeNode(volume);

        //split into quadrants
        var quarterExtents = volume.HalfExtents / 2;

        float left = volume.Position.X - quarterExtents.X;
        float right = volume.Position.X + quarterExtents.X;
        float far = volume.Position.Z - quarterExtents.Z;
        float near = volume.Position.Z + quarterExtents.Z;

        var quadrant1 = new Volume_AABB(new(left, 0, far), quarterExtents);
        var quadrant2 = new Volume_AABB(new(right, 0, far), quarterExtents);
        var quadrant3 = new Volume_AABB(new(left, 0, near), quarterExtents);
        var quadrant4 = new Volume_AABB(new(right, 0, near), quarterExtents);

        node.children[0] = ConstructForTerrain(worldSpawn, quadrant1, depth - 1);
        node.children[1] = ConstructForTerrain(worldSpawn, quadrant2, depth - 1);
        node.children[2] = ConstructForTerrain(worldSpawn, quadrant3, depth - 1);
        node.children[3] = ConstructForTerrain(worldSpawn, quadrant4, depth - 1);

        return node;

    }
}



public class QuadTreeNode
{

    public readonly Volume_AABB volume;
    public readonly QuadTreeNode[] children = new QuadTreeNode[4];
    public readonly Renderer[]? renderers = null;
    public readonly Collider[]? colliders = null;
    public readonly bool isLeaf;

    public QuadTreeNode(Volume_AABB volume)
    {
        this.volume = volume;
        isLeaf = false;
        children = new QuadTreeNode[4];
    }

    public QuadTreeNode(Volume_AABB volume, Renderer[] renderers, Collider[] colliders)
    {
        this.volume = volume;
        isLeaf = true;
        this.renderers = renderers;
        this.colliders = colliders;
    }

}