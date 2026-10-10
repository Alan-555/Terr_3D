using System.Text;
using OpenTK.Graphics.OpenGL4;
using Prowl.Slang;
namespace Terr3D.Client.Resources.Importers;


public class ShaderImporter : Importer<ShaderProgram>
{

    protected override ShaderProgram Import(Stream data, string path)
    {
        path = path[0.. (path.IndexOf('.'))];

        var v = LoadShader($"{path}.vert.glsl", ShaderType.VertexShader);
        var f = LoadShader($"{path}.frag.glsl", ShaderType.FragmentShader);
        return new(0, v, f);

    }

    static Shader LoadShader(string name, ShaderType shaderType)
    {
        var shaderContent =  File.ReadAllText("res/"+name);
        Shader shader = new(shaderType, shaderContent);
        return shader;
    }
}


/*

        /*path = ResourceManager.GetActualPath(path);
        var target = new TargetDescription
        {
            Format = CompileTarget.Glsl,
            Profile = GlobalSession.FindProfile("glsl_330"),
        };
        var session = GlobalSession.CreateSession(new SessionDescription
        {
            Targets = [target],
            SearchPaths = [Path.GetDirectoryName(Path.GetFullPath(path))!],
        });

        var module = session.LoadModule(Path.GetFileNameWithoutExtension(path), out var diag);
        var vs = module.FindEntryPointByName("vertexMain");
        var fs = module.FindEntryPointByName("fragmentMain");
        var program = session.CreateCompositeComponentType([module, vs, fs], out diag);

        string vert = Encoding.UTF8.GetString(program.GetEntryPointCode(0, 0, out diag).Span).TrimEnd('\0');
        string frag = Encoding.UTF8.GetString(program.GetEntryPointCode(1, 0, out diag).Span).TrimEnd('\0');

        // 3. Hand off to your existing path
        var sp = new ShaderProgram(flags,
            new Shader(ShaderType.VertexShader, vert),
            new Shader(ShaderType.FragmentShader, frag));
        sp.State = state;      // new field on ShaderProgram, applied when bound
        return sp;*/