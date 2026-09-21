using OpenTK.Mathematics;
using Terr3D.Client.Resources;

namespace Terr3D.Server.WorldGen;


public static class WorldObjects
{

    public const int Tree = 1;
    public const int Rock = 2;
    public const int Dragon = 3;

    public static readonly Dictionary<int, string> Models = new()
    {
        {Tree, "tree"},
        {Rock, "cave"},
        {Dragon, "dragon"}
    };
}