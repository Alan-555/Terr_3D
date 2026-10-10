using Terr3D.Client;
using Terr3D.Server.Core;
using Terr3D.Utils;

namespace Terr3D;
class Program
{
    public static bool DEBUG_FLAG { get; private set; }
    static void Main(string[] args)
    {
        if (args.Contains("--debug"))
        {
            Diagnostics.Debug("Debug mode enabled");
            DEBUG_FLAG = true;
        }
        using var engine = new EngineInternal();
    }

    
}