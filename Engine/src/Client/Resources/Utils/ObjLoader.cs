using OpenTK.Mathematics;
using Terr3D.Utils;
using static System.Globalization.CultureInfo;

namespace Terr3D.Client.Resources.Utils;

/// <summary>
/// Loads a .obj file and parses its vertices and triangles
/// </summary>
public static class ObjLoader
{
    public static (Vertex[], Triangle[]) Load(string filename)
    {
        var lines = File.ReadAllLines(filename);
        
        //Temporary lists for raw OBJ data
        List<Vector3> tempPositions = new();
        List<Vector2> tempUVs = new();
        List<Vector3> tempNormals = new();

        //OpenGL buffers
        List<Vertex> vertices = new();
        List<Triangle> triangles = new();
        
        
        Dictionary<string, uint> vertexCache = new();

        foreach (var line in lines)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            //vertex
            if (parts[0] == "v")
            {
                //Try parse the vertex data
                if (float.TryParse(parts[1], InvariantCulture, out float x) && 
                    float.TryParse(parts[2], InvariantCulture, out float y) && 
                    float.TryParse(parts[3], InvariantCulture, out float z))
                {
                    //Add the vertex data
                    tempPositions.Add(new Vector3(x, y, z));
                }
            }
            //Texture UVs
            else if (parts[0] == "vt")
            {
                //Try parse the UV
                if (float.TryParse(parts[1], InvariantCulture, out float u) && 
                    float.TryParse(parts[2], InvariantCulture, out float v))
                {
                    //Add the UVs on successful parse
                    tempUVs.Add(new Vector2(u, v));
                }
            }
            //Normals
            else if (parts[0] == "vn")
            {
                //Try parse the vertex normal
                if (float.TryParse(parts[1], InvariantCulture, out float x) && 
                    float.TryParse(parts[2], InvariantCulture, out float y) && 
                    float.TryParse(parts[3], InvariantCulture, out float z))
                {
                    //Cache the vertex normal
                    tempNormals.Add(new Vector3(x, y, z));
                }
            }
            //Faces
            else if (parts[0] == "f") 
            {
                //Check for triangulated faces
                if (parts.Length == 4) 
                {
                    uint[] faceIndices = new uint[3];

                    //For each triangle vertex
                    for (int i = 0; i < 3; i++)
                    {
                        string vertexDef = parts[i + 1];

                        //If we already built this exact vertex, reuse its index
                        if (vertexCache.TryGetValue(vertexDef, out uint existingIndex))
                        {
                            faceIndices[i] = existingIndex;
                        }
                        else
                        {
                            //Parse the v/vt/vn string
                            var indices = vertexDef.Split('/');
                            
                            //OBJ indices start with 1, so subtract 1
                            int posIndex = int.Parse(indices[0]) - 1;
                            Vector3 pos = tempPositions[posIndex];
                            
                            //Try reading the uv
                            Vector2 uv = Vector2.Zero;
                            if (indices.Length > 1 && !string.IsNullOrEmpty(indices[1]))
                            {
                                uv = tempUVs[int.Parse(indices[1]) - 1];
                            }

                            //Try reading the normal
                            Vector3 normal = Vector3.Zero;
                            if (indices.Length > 2 && !string.IsNullOrEmpty(indices[2]))
                            {
                                normal = tempNormals[int.Parse(indices[2]) - 1];
                            }

                            //Create and store the new compiled vertex
                            Vertex newVert = new(pos, normal, uv);
                            vertices.Add(newVert);
                            
                            uint newIndex = (uint)(vertices.Count - 1);
                            faceIndices[i] = newIndex;
                            vertexCache[vertexDef] = newIndex; //Cache it, should it be a shared vertex
                        }
                    }

                    triangles.Add(new Triangle() { i0 = faceIndices[0], i1 = faceIndices[1], i2 = faceIndices[2] });
                }
                else
                {
                    Diagnostics.Warn("The model has a face that is not triangulated! Discarding...");
                }
            }
        }
        return ([..vertices], [.. triangles]);
    }
}