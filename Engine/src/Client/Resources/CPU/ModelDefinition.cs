using YamlDotNet.Serialization;


namespace Terr3D.Client.Resources;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
public class ModelDefinition
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; }


    [YamlMember(Alias = "mesh")]
    public string Mesh { get; set; }


    [YamlMember(Alias = "properties")]
    public EntityProperties Properties { get; set; }


    [YamlMember(Alias = "material")]
    public MaterialDefinition Material { get; set; }


    [YamlMember(Alias = "colliders")]
    public List<ColliderDefinition> Colliders { get; set; }
}

public class MaterialDefinition
{
    [YamlMember(Alias = "shader")]
    public string Shader { get; set; }

    [YamlMember(Alias = "properties")]
    public MaterialProperties Properties { get; set; }
}

public class MaterialProperties
{
    [YamlMember(Alias = "albedo")]
    public string Albedo { get; set; }

    [YamlMember(Alias = "diffuse")]
    public List<float> Diffuse { get; set; }

    [YamlMember(Alias = "specular")]
    public List<float> Specular { get; set; }

    [YamlMember(Alias = "shininess")]
    public float Shininess { get; set; }
}

public class EntityProperties
{
    [YamlMember(Alias = "is_static")]
    public bool IsStatic { get; set; }
}

public class ColliderDefinition
{
    [YamlMember(Alias = "type")]
    public string Type { get; set; }

    [YamlMember(Alias = "center")]
    public List<float> Center { get; set; }

    [YamlMember(Alias = "half_extents")]
    public List<float> HalfExtents { get; set; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.