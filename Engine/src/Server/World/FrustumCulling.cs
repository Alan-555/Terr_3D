using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using Terr3D.Server.Shared;
using Terr3D.Server.Components;
using Terr3D.Server.Entities;

namespace Terr3D.Server.Engine;


public static class FrustumCulling
{

    public static QuadTreeNode Construct(Scene scene, float cellSize, int numChunks)
    {
        float size = cellSize * numChunks;

        int depth = (int)Math.Log(numChunks, 4); //assuming numChunks is divisible by four
        var halfSize = size / 2f;
        var rootVolume = new Bounds(Vector3.Zero, new(halfSize));
        return Construct(scene, rootVolume, depth);
    }

    private static QuadTreeNode Construct(Scene scene, Bounds volume, int depth)
    {
        if (depth <= 0)
        {
            //now we find all intersecting objects
            var objects = scene.SceneRegistry.GetComponents<ISpatialBounds>();
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;

            List<Renderer> renderers = new();
            List<Collider> colliders = new();

            foreach (var obj in objects)
            {
                var objBounds = obj.GetBounds();
                if (objBounds.IntersectsInfHeight(volume))
                {

                    if(obj is Renderer r)
                    {
                        if(r.rendererClass != RendererClass.RENDERER_STATIC) continue;

                        renderers.Add(r);
                    }
                    else if (obj is Collider c)
                    {
                        if(!c.IsStatic) continue;
                        colliders.Add(c);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Only Renderers and Colliders can implement {nameof(ISpatialBounds)}.");
                    }

                    
                    if (objBounds.MaxPoint.Y > maxY)
                        maxY = objBounds.MaxPoint.Y;
                    if (objBounds.MinPoint.Y < minY)
                        minY = objBounds.MinPoint.Y;
                }
            }

            //calculate the height data
            var halfHeight = (maxY - minY) / 2f;
            float y = minY + halfHeight;


            volume.Position.Y = y;
            volume.HalfExtents.Y = halfHeight;

            //return the leaf
            return new(volume, [.. renderers], [.. colliders]);
        }

        var node = new QuadTreeNode(volume);

        //split into quadrants
        var quarterExtents = volume.HalfExtents / 2;

        float left = volume.Position.X - quarterExtents.X;
        float right = volume.Position.X + quarterExtents.X;
        float far = volume.Position.Z - quarterExtents.Z;
        float near = volume.Position.Z + quarterExtents.Z;

        var quadrant1 = new Bounds(new(left, 0, far), quarterExtents);
        var quadrant2 = new Bounds(new(right, 0, far), quarterExtents);
        var quadrant3 = new Bounds(new(left, 0, near), quarterExtents);
        var quadrant4 = new Bounds(new(right, 0, near), quarterExtents);

        node.children[0] = Construct(scene, quadrant1, depth - 1);
        node.children[1] = Construct(scene, quadrant2, depth - 1);
        node.children[2] = Construct(scene, quadrant3, depth - 1);
        node.children[3] = Construct(scene, quadrant4, depth - 1);

        return node;

    }
}



public class QuadTreeNode
{

    public readonly Bounds volume;
    public readonly QuadTreeNode[] children = new QuadTreeNode[4];
    public readonly Renderer[]? renderers = null;
    public readonly Collider[]? colliders = null;
    public readonly bool isLeaf;

    public QuadTreeNode(Bounds volume)
    {
        this.volume = volume;
        isLeaf = false;
        children = new QuadTreeNode[4];
    }

    public QuadTreeNode(Bounds volume, Renderer[] renderers, Collider[] colliders)
    {
        this.volume = volume;
        isLeaf = true;
        this.renderers = renderers;
        this.colliders = colliders;
    }

}