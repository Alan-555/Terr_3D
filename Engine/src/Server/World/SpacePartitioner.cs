using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using Terr3D.Server.Components;
using Terr3D.Server.Entities;
using Terr3D.Server.Shared;

namespace Terr3D.Server.World;


public class SpacePartitioner
{

    readonly QuadTreeNode _rootNode;

    public SpacePartitioner(Terrain worldSpawn)
    {
        _rootNode = FrustumCulling.ConstructForTerrain(worldSpawn);
    }


    public List<Renderer> QueryFrustum(Matrix4 viewProjMatrix)
    {
        List<Renderer> renderers = new();
        QueryFrustum(viewProjMatrix, renderers, _rootNode);
        return renderers;
    }

    public Collider[] QueryCollision(Volume_AABB queryVolume)
    {
        List<Collider> colliders = new();
        QueryCollision(queryVolume, colliders, _rootNode);
        return [..colliders];
    }

    /// <summary>
    /// Casts the passed ray
    /// </summary>
    /// <param name="ray">The ray to raycast with</param>
    /// <param name="hit">The hit info</param>
    /// <returns> true if it hit something, false otherwise</returns>
    public bool Raycast(Ray ray, out RaycastHit hit)
    {
        hit = RaycastHit.NoHit;
        return Raycast(ray, ref hit, _rootNode);
    }

    private bool Raycast(Ray ray, ref RaycastHit closestHit, QuadTreeNode node)
    {
        //if the ray does not intersect with the node at all, exit early
        if (!node.volume.Intersects(ray, out float distance))
            return false;

        //if the distance is further than the closest hit, return
        if (distance >= closestHit.Distance)
            return false;

        bool hitAnything = false;

        //if it is a leaf we iterate the leaf colliders and raycast them
        if (node.isLeaf)
        {
            foreach (var collider in node.colliders!)
            {
                if (collider.Raycast(ray, out var hit))
                {
                    if (hit.Distance < closestHit.Distance)
                    {
                        closestHit = hit;
                        hitAnything = true;
                    }
                }
            }
        }
        else
        {
            //sort the intersecting children based on distance
            (QuadTreeNode node, float dist)[] childDistances = new (QuadTreeNode, float)[4];
            for (int i = 0; i < 4; i++)
            {
                if (node.children[i].volume.Intersects(ray, out float d))
                    childDistances[i] = (node.children[i], d);
                else
                    childDistances[i] = (node.children[i], float.MaxValue);
            }
            for (int i = 0; i < 3; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    if (childDistances[i].dist > childDistances[j].dist)
                    {
                        (childDistances[i], childDistances[j]) = (childDistances[j], childDistances[i]);
                    }
                }
            }

            foreach (var child in childDistances)
            {
                //the child is behind our closest hit - break
                if (child.dist >= closestHit.Distance) break;

                //test child
                if (Raycast(ray, ref closestHit, child.node))
                {
                    hitAnything = true;
                }
            }
        }

        return hitAnything;
    }

    void QueryFrustum(Matrix4 viewProjMatrix, List<Renderer> renderers, QuadTreeNode node)
    {
        var classification = Volume_AABB.ClassifyVisibilityForFrustum(viewProjMatrix, node.volume);
        if (classification == Visibility.NONE)
        {
            //do not bother searching further. Cull this entire node
            return;
        }
        if (classification == Visibility.FULL)
        {
            //we can see the whole sub-tree. Do not bother checking further
            AddAllLeafs(renderers, node);
            return;
        }

        //if it is a leaf there's no further subdivision
        if (node.isLeaf)
        {
            AddAllLeafs(renderers, node);
            return;
        }

        //we are not sure. Keep searching

        foreach (var child in node.children)
        {
            QueryFrustum(viewProjMatrix, renderers, child);
        }
    }

    

    public void QueryCollision(Volume_AABB queryVolume, List<Collider> results, QuadTreeNode node)
    {
        //Skip this sub-tree entirely
        if (!node.volume.Intersects(queryVolume))
            return;

        //If it is a leaf, check collision with all of them
        if (node.isLeaf)
        {
            foreach (var c in node.colliders!)
            {
                if (c.Intersects(queryVolume))
                    results.Add(c);
            }
            return;
        }

        //Descend the tree
        foreach (var child in node.children)
        {
            QueryCollision(queryVolume, results, child);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddAllLeafs(List<Renderer> renderers, QuadTreeNode node)
    {
        if (node.isLeaf)
        {
            renderers.AddRange(node.renderers!);
            return;
        }

        foreach (var child in node.children)
        {
            AddAllLeafs(renderers, child);
        }

    }

    
}

