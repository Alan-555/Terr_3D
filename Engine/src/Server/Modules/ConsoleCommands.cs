using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using OpenTK.Mathematics;
using Terr3D.Client;
using Terr3D.Server.Components;
using Terr3D.Server.Entities;
using Terr3D.Server.Engine;
using Terr3D.Utils;



namespace Terr3D.Server.Modules;

using WfAndTarget = (ISupportsWireframe supports, WireframeBoxRenderer? wfRenderer);


/// <summary>
/// Handles the execution of console commands
/// </summary>
public class DebugConsole
{

    const string _Player = "plr";
    const string _Ent = "ent";
    const string _Env = "env";
    const string _Debug = "dbg";
    const string _Server = "sv";
    const string _Client = "cl";

    Scene scene;

    public DebugConsole(Scene scene)
    {
        this.scene = scene;
    }

    /// <summary>
    /// Runs the command and the args
    /// </summary>
    /// <param name="command">The commands</param>
    /// <param name="args">The args array</param>
    /// <returns><Command output or null if none/returns>
    public string? RunCommand(string command, params string[] args)
    {
        var method = GetCommand(command);
        if (method == null)
        {
            Diagnostics.Error($"Command {command} not found");
            return null;
        }

        try
        {
            var convertedArgs = ProcessParameters(method, args);
            return method.Invoke(this, convertedArgs)?.ToString();
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
                ex = ex.InnerException;
            Diagnostics.Error($"Exception running the command.\n{ex.Message}");
            return "\\e" + ex.Message;
        }


    }

    private string GetCommandHelp(string command)
    {
        StringBuilder help = new();
        var method = GetCommand(command) ?? throw new Exception("Command not known");
        var cmd = method.GetCustomAttribute<ConsoleCommandAttribute>()!;
        var parameters = method.GetParameters();

        help.Append("Args: ");

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].IsOptional)
            {
                help.Append("[");
            }
            if (parameters[i].ParameterType == typeof(object[]))
            {
                help.Append("...");
            }

            help.Append($"{parameters[i].Name}: {parameters[i].ParameterType}");

            if (parameters[i].IsOptional)
            {
                help.Append("]");
            }

            if (i != parameters.Length - 1)
                help.Append(", ");
        }

        help.AppendLine("\n" + cmd.Description);

        return help.ToString();
    }

    private MethodInfo? GetCommand(string command)
    {
        return GetType().GetMethods().FirstOrDefault(m => m.GetCustomAttribute<ConsoleCommandAttribute>()?.Names.Contains(command) ?? false);
    }

    private object[] ProcessParameters(MethodBase method, string[] args, bool skipFirst = false)
    {
        var parameters = method.GetParameters();
        List<object> result = new();
        bool skippedFirst = false;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (skipFirst && !skippedFirst)
            {
                skippedFirst = true;
                continue;
            }
            if (parameters[i].IsOptional && args.Length <= i)
            {
                result.Add(parameters[i].DefaultValue!);
                continue;
            }
            if (parameters[i].ParameterType == typeof(string[]))
            {
                //args
                result.Add(args[i..]);
                break;
            }
            result.Add(Convert.ChangeType(args[skippedFirst ? i - 1 : i], parameters[i].ParameterType, CultureInfo.InvariantCulture));
        }

        return [.. result];
    }

    public Type[] GetAllClassesOf<T>()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        Type baseType = typeof(T);

        return [..assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(baseType))];
    }

#if false

    public Entities.Entity? GetEntity(string name)
    {
        if (name == "!picker")
        {
            if (scene.Globals.Player.LocalPlayer.Raycast(out var hit))
            {
                return hit.Collider!.Entity;
            }
            else
            {
                throw new Exception("Nothing picked");
            }
        }
        return scene.SceneRegistry.Entities.FirstOrDefault(e => e.Name == name);
    }

    //Player commands

    [ConsoleCommand(_Player, "tp", "Teleports player to desired coordinates")]
    public void PlayerTP(float x, float y, float z)
    {
        scene.Globals.Player.PlayerCam.Transform.Parent!.Transform.Position = new(x, y, z);
    }

    //Entity commands

    [ConsoleCommand(_Ent, "remove", "Kills the specified entity")]
    public void SceneKill(string name)
    {
        var ent = GetEntity(name) ?? throw new("Entity not found");
        ent.Destroy();
    }

    [ConsoleCommand(_Ent, "removec", "Removes a component from an entity")]
    public void SceneKillC(string name, string classNameComponent)
    {
        var compType = GetAllClassesOf<Components.Component>().FirstOrDefault(t => t.Name == classNameComponent);
        var entity = GetEntity(name) ?? throw new("Entity not found");

        foreach (var c in entity.components)
        {
            if (c.GetType() == compType)
            {
                c.Destroy();
            }
        }
    }

    [ConsoleCommand(_Ent, "tp", "Teleports an entity to a specified location")]
    public void SceneTp(string entName, float x, float y, float z)
    {
        var ent = GetEntity(entName) ?? throw new("Entity not found");
        ent.Transform.Position = (x, y, z);
    }

    [ConsoleCommand(_Ent, "tp_local", "Teleports an entity to its local coordinates")]
    public void SceneTpLoc(string entName, float x, float y, float z)
    {
        var ent = GetEntity(entName) ?? throw new("Entity not found");
        ent.Transform.LocalPosition = (x, y, z);
    }


    [ConsoleCommand(_Ent, "dump", "Dumps all dynamic/static entities")]
    public string SceneEntDump(bool reqStatic = false)
    {
        string ret = "";
        foreach (var entity in scene.SceneRegistry.Entities)
        {
            if (reqStatic && !entity.IsStatic || !reqStatic && entity.IsStatic) continue;
            ret += entity.ToString() + "\n";
        }

        return ret;
    }

    [ConsoleCommand(_Ent, "parent", "Sets the parent of an entity")]
    public string EntParentTo(string child, string parent)
    {
        var otherEnt = GetEntity(parent) ?? throw new("Entity not found");
        var targetEnt = GetEntity(child) ?? throw new("Entity not found");
        targetEnt.Transform.SetParent(otherEnt.Transform);

        return "";
    }

    [ConsoleCommand(_Ent, "create", "Creates a new entity")]
    public void SceneEntNew(string name, params string[] args)
    {
        var entType = GetAllClassesOf<Entities.Entity>().FirstOrDefault(t => t.Name == name);

        if (entType != null)
        {
            var proc = ProcessParameters(entType.GetConstructors().First(), args, true);
            object[] realArgs = [scene, .. proc];
            object? instance = Activator.CreateInstance(entType, realArgs) ?? throw new Exception("Instance is null");
            return;
        }

        throw new Exception("No such entity");
    }

    [ConsoleCommand(_Ent, "enabled", "Disables/enables an entity")]
    public void SceneDisable(string name, bool enable)
    {
        var entity = GetEntity(name) ?? throw new Exception("No such entity");
        entity.SetEnabled(enable);
    }

    [ConsoleCommand(_Ent, "enabledc", "Disables/enables a component")]
    public void SceneDisablec(string name, int compI,  bool enable)
    {
        var entity = GetEntity(name) ?? throw new Exception("No such entity");
        var comp = entity.components[compI];
        comp.SetEnabled(enable);
    }

    [ConsoleCommand(_Ent, "bind", "Attaches a new component to an entity")]
    public void SceneNewComponent(string entName, string classname, params string[] args)
    {
        var compType = GetAllClassesOf<Components.Component>().FirstOrDefault(t => t.Name == classname);
        var entity = GetEntity(entName) ?? throw new("Entity not found");
        if (compType != null)
        {
            object? instance = Activator.CreateInstance(compType, args) ?? throw new Exception("Instance is null");
            var comp = (Components.Component)instance;
            var addCmpMethod = typeof(Entities.Entity).GetMethod("AddComponent_")!.MakeGenericMethod(compType);

            addCmpMethod.Invoke(entity, [comp]);
            return;
        }

        throw new Exception("No such component");
    }

    [ConsoleCommand(_Ent, "info", "Gets info about the entity")]
    public string SceneInfo(string entName)
    {
        var entity = GetEntity(entName) ?? throw new("Entity not found");

        return SceneInfo(entity);
    }

    private string SceneInfo(Entity entity)
    {
        StringBuilder childInfo = new();

        if (entity.children.Count != 0)
        {
            foreach (var child in entity.children)
            {
                string s = SceneInfo(child.Entity);
                s = s.Replace("\n\r", "\n");
                s = s.Replace("\n", "\n   ");
                childInfo.Append("   " + s);

                childInfo.Append("\n");
            }
            childInfo.Append("Children:\n");
        }

        StringBuilder compInfo = new();

        if (entity.components.Count != 0)
        {
            int i = 0;
            foreach (var comp in entity.components)
            {
                compInfo.Append($"   ({i++})" + comp.GetType());
                compInfo.Append("\n");
            }
            compInfo.Append("Components:\n");
        }

        return $@"{childInfo}
Parent: {entity.Transform._parent?.Entity.ToString() ?? "orphan"}
{compInfo}
Enabled: {entity.IsEnabled} | Static: {entity.IsStatic} | Destroyed: {entity.IsDestroyed}
{entity}
";
    }

    //sv commands

    [ConsoleCommand(_Server, "timescale", "Sets the timescale. This only affects deltatiming!")]
    public void SvTimescale(float timeScale)
    {
        EngineWindow.Instance.TimeScale = timeScale;
    }

    //cl commands
    [ConsoleCommand(_Client, "usecam", "Uses the specified camera for rendering")]
    public void ClUsecam(string name)
    {
        var e = GetEntity(name) ?? throw new Exception("No such entity");
        var c = e.components.OfType<Camera>().FirstOrDefault() ?? throw new Exception("This entity has no camera!");
        scene.Globals.CurrentCamera = c;

    }

    //Debug commands

    [ConsoleCommand(_Debug, "cull", "Uses the player camera for culling calculation, even if other camera is used.")]
    public void DebugPlayerCull()
    {
        scene.Globals.Worldspawn.debugCulling = !scene.Globals.Worldspawn.debugCulling;
        Player.debugCull = !Player.debugCull;
    }

    [ConsoleCommand(_Debug, "phybox", "Spawns a physics box")]
    public void DebugSpawnBox()
    {
        var box = new Entities.DebugEntity(scene, "box" + (int)EngineWindow.Time, Client.Resources.ResourceManager.Meshes[Client.Resources.ResourceIndex.Meshes.Cube], false);
        box.Transform.Position = scene.Globals.Player.PlayerCam.Transform.Position + (0, 10, 0);
        var p = box.AddComponent<Components.Physics>();
        p.Collider = box.AddComponent(new AABB_Collider(new Vector3(1, 1, 1), default));
    }

    bool _showCol = false;
    bool _showMisc = false;

    [ConsoleCommand(_Debug, "show", "Toggles the specified type of debug view. 'col' for colliders, 'misc' for other")]
    public void DebugSee(string type)
    {
        if (type == "col" || type == "collider")
        {
            _showCol = !_showCol;
            var cache = scene.SceneRegistry.Entities;
            foreach (var ent in cache)
            {
                var supporters = ent.components.OfType<Collider>().Cast<ISupportsWireframe>().ToArray();
                ShowDebugFor(ent, supporters, WireframeBoxRenderer.ColourCollider, _showCol);
            }
        }
        else if (type == "misc")
        {
            _showMisc = !_showMisc;
            

        }

    }

    private void ShowDebugFor(Entity ent, ISupportsWireframe[] supporters, Vector3 colour, bool doShow)
    {
        List<WfAndTarget> list = [.. supporters.Select(c => new WfAndTarget(c, null))];
        WfAndTarget[] tuples = [.. list];
        var renderers = ent.components.OfType<WireframeBoxRenderer>();
        foreach (var renderer in renderers)
        {
            for (int i = 0; i < tuples.Length; i++)
            {
                if (renderer.Target == tuples[i].supports)
                {
                    tuples[i].wfRenderer = renderer;
                    tuples[i].wfRenderer?.SetEnabled(doShow);
                    break;
                }
            }
        }

        for (int i = 0; i < tuples.Length; i++)
        {
            if (tuples[i].wfRenderer == null)
            {
                var r = new WireframeBoxRenderer(tuples[i].supports, colour, false);
                ent.AddComponent(r);
                r.SetEnabled(doShow);
            }
        }
    }

    //Meta commands

    [ConsoleCommand("classlist", "Dumps all Entity classes")]
    public string EntityList()
    {
        var entTypes = GetAllClassesOf<Entities.Entity>();
        string ret = "";
        foreach (var entity in entTypes)
        {
            ret += entity.Name + "\n";
        }

        return ret;
    }

    [ConsoleCommand("classinfo", "Gets an info about a specified class")]
    public string ClassInfo(string name)
    {
        var e = GetAllClassesOf<Entities.Entity>().First(e => e.Name == name);
        StringBuilder ret = new();
        foreach (var c in e.GetConstructors())
        {
            foreach (var param in c.GetParameters().Reverse())
            {
                ret.Append($"{param.Name}: {param.ParameterType.Name}\n");
            }
        }

        return ret.ToString();
    }

    [ConsoleCommand("classlistc", "Dumps all Component classes")]
    public string ComponentList()
    {
        var compTypes = GetAllClassesOf<Components.Component>();
        string ret = "";
        foreach (var entity in compTypes)
        {
            ret += entity.Name + "\n";
        }

        return ret;
    }

    [ConsoleCommand("classinfoc", "Gets an info about a specified class")]
    public string ClassInfoc(string name)
    {
        var e = GetAllClassesOf<Components.Component>().First(e => e.Name == name);
        StringBuilder ret = new();
        foreach (var c in e.GetConstructors())
        {
            foreach (var param in c.GetParameters().Reverse())
            {
                ret.Append($"{param.Name}: {param.ParameterType.Name}\n");
            }
        }

        return ret.ToString();
    }

    [ConsoleCommand("echo", "Echoes")]
    public string Echo(string str)
    {
        return str;
    }

    [ConsoleCommand(["exit", "quit", "term"], "Stops the engine")]
    public string EngineQuit()
    {
        System.Environment.Exit(0);
        return "Bye :3";
    }

    [ConsoleCommand(["help", "?"], "Gets help for the supplied command")]
    public string ConsoleHelp(string cmd)
    {
        var method = GetCommand(cmd);
        if (method == null)
        {
            return "Command not known";
        }

        return GetCommandHelp(cmd);
    }

    [ConsoleCommand(["list", "commands"], "Lists all commands")]
    public string ConsoleListCommands()
    {
        var methods = GetType().GetMethods().Select(m => m.GetCustomAttribute<ConsoleCommandAttribute>()!).Where(m => m != null);
        if (methods == null)
        {
            return "Commands not known";
        }

        var str = "";
        foreach (var method in methods)
        {
            str += $"{string.Join(',', method.Names)} -> {method.Description}\n";
        }
        return str;
    }

    [ConsoleCommand(["sceneNew"], "Creates a new scene")]
    public string SceneNew()
    {
        scene.DestroyScene();
        Program.NewScene();
        return "";
    }
#endif
}


[AttributeUsage(AttributeTargets.Method)]
class ConsoleCommandAttribute : Attribute
{
    public string[] Names { get; }
    public string Description { get; }

    public ConsoleCommandAttribute(string cmdClass, string name, string description)
    {
        Names = [$"{cmdClass}_{name}"];
        Description = description;
    }

    public ConsoleCommandAttribute(string name, string description)
    {
        Names = [$"{name}"];
        Description = description;
    }

    public ConsoleCommandAttribute(string cmdClass, string[] nameAndAliases, string description)
    {
        for (int i = 0; i < nameAndAliases.Length; i++)
            nameAndAliases[i] = $"{cmdClass}_{nameAndAliases[i]}";
        Names = nameAndAliases;
        Description = description;
    }

    public ConsoleCommandAttribute(string[] nameAndAliases, string description)
    {
        Names = nameAndAliases;
        Description = description;
    }
}
