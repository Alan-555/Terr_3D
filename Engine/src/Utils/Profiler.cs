using System.Diagnostics;

namespace Terr3D.Utils;

public static class Profiler
{
    private class ActivePhase
    {
        public ProfilerPhase Id { get; set; }
        public Stopwatch Timer { get; set; }
    }

    private static Dictionary<ProfilerPhase, ProfilerPhase> _hierarchy;
    private static Stack<ActivePhase> _stack;

    public static void Initialise()
    {
        _stack = new Stack<ActivePhase>();

        _hierarchy = new Dictionary<ProfilerPhase, ProfilerPhase>
        {
            {ProfilerPhase.ROOT, ProfilerPhase.ROOT},
            {ProfilerPhase.SCENE_UPDATE, ProfilerPhase.ROOT},
            {ProfilerPhase.FRAME_PREPARE, ProfilerPhase.ROOT},
            {ProfilerPhase.FRAME_RESOLVE_STATIC, ProfilerPhase.FRAME_PREPARE},
            {ProfilerPhase.FRAME_RESOLVE_DYNAMIC, ProfilerPhase.FRAME_PREPARE},
            {ProfilerPhase.FRAME_RENDER, ProfilerPhase.ROOT},
            {ProfilerPhase.FRAME_RENDER_GEOMETRY, ProfilerPhase.FRAME_RENDER},
            {ProfilerPhase.FRAME_RENDER_UI, ProfilerPhase.FRAME_RENDER},
        };
    }

    public static void Start(ProfilerPhase id)
    {
        if(!Program.DEBUG_FLAG || true) return;
        if (!_hierarchy.TryGetValue(id, out ProfilerPhase requiredParent))
        {
            throw new ArgumentException($"ProfilerPhase {id} is not defined in the hierarchy.");
        }

        ProfilerPhase currentActive = _stack.Count > 0 ? _stack.Peek().Id : ProfilerPhase.ROOT;

        while (currentActive != requiredParent)
        {
            if (_stack.Count == 0)
            {
                throw new InvalidOperationException($"Hierarchy mismatch. Reached root without finding parent {requiredParent} for {id}.");
            }

            EndCurrent();
            currentActive = _stack.Count > 0 ? _stack.Peek().Id : ProfilerPhase.ROOT;
        }

        var newBlock = new ActivePhase { Id = id, Timer = Stopwatch.StartNew() };
        _stack.Push(newBlock);
    }

    private static void EndCurrent()
    {
        if(!Program.DEBUG_FLAG || true) return;
        if (_stack.Count == 0) return;

        var block = _stack.Pop();
        block.Timer.Stop();

        Log($"{block.Id} ({block.Timer.Elapsed.TotalMicroseconds}µs)", _stack.Count + 1);
    }

    public static void EndAll()
    {
        if(!Program.DEBUG_FLAG || true) return;
        while (_stack.Count > 0)
        {
            EndCurrent();
        }
        Console.WriteLine("------------------END FRAME------------------");
    }

    private static void Log(string message, int? indentOverride = null)
    {
        int depth = indentOverride ?? _stack.Count;
        string indent = new string(' ', (depth - 1) * 4);
        Console.WriteLine($"{indent}{message}");
    }

}
public enum ProfilerPhase
{
    ROOT,
    SCENE_UPDATE,
    FRAME_PREPARE,
        FRAME_RESOLVE_STATIC,
        FRAME_RESOLVE_DYNAMIC,
    FRAME_RENDER,
        FRAME_RENDER_GEOMETRY,
        FRAME_RENDER_UI,
}