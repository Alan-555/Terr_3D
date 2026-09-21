using System.Text;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Entities;
using Terr3D.Server.World;

namespace Terr3D.Server.Components;

/// <summary>
/// Class responsible for rendering text on the screen. Very inefficient
/// </summary>
public class FontRenderer : Component
{
    const int CharSize = 12; //The size of the quad on the screen
    const int LineSpacing = 0; //Extra space between lines
    const int NumChars = 16; //The number of characters per axis in the font atlas

    const float OffsetX = AtlasSize / NumChars; //font atlas X stride
    const float OffsetY = AtlasSize / NumChars - 0.5f; //font atlas Y stride
    const float AtlasSize = 1024;

    const float ScaleX = OffsetX / AtlasSize * 0.3f; //The size of a letter in the atlas
    const float ScaleY = OffsetY / AtlasSize * 0.1f;

    const float Aspect = ScaleX / ScaleY; //The aspect of a letter

    ShaderProgram fontShader = ResourceManager.Shaders[ResourceIndex.Shaders.Font];
    Mesh quad = ResourceManager.Meshes[ResourceIndex.Meshes.Quad];

    readonly List<(int, string)> parts = new();

    /// <summary>
    /// Is the console to be rendered?
    /// </summary>
    public bool DoShowConsole { get; set; }

    /// <summary>
    /// The console content
    /// </summary>
    public string Console { get; set; } = "";

    /// <summary>
    /// Should the debug menu be drawn and tank the framerate?
    /// </summary>
    public bool DrawDebug { get; set; } = Program.DEBUG_FLAG;

    /// <summary>
    /// The colour the text is drawn with
    /// </summary>
    public Vector3 Colour = Vector3.One;

    /// <summary>
    /// Renders a debug label on the screen for this frame
    /// </summary>
    /// <param name="text">The text to render</param>
    /// <param name="order">The order. Lower = higher</param>
    public void RenderLabel(string text, int order)
    {
        if(DrawDebug)
            parts.Add((order, text));
    }


    /// <summary>
    /// Yields all quads to render
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Renderer> PrepareFrame()
    {
        if (DrawDebug)
        {
            parts.Sort((x, y) => x.Item1 - y.Item1);
            string _text = string.Join("\n\n", parts.Select(x => x.Item2));
            foreach (var r in RenderText(_text, 0, 0))
                yield return r;
        }
        if (DoShowConsole)
            foreach (var r in RenderText(Console + "_", 0, EngineWindow.Instance.ClientSize.Y - CharSize * Aspect - 10, false))
                yield return r;

    }

    /// <summary>
    /// Yields all quads to render for a string
    /// </summary>
    /// <returns></returns>
    IEnumerable<Renderer> RenderText(string text, float x, float y, bool flowDown = true)
    {
        text = text.Replace("\r", "");
        foreach (char c in text)
        {
            if (c == '\n')
            {
                x = 10000; //dirty force skip (nobody has that large of a monitor, right???)
            }
            //Check if we are to wrap the line
            if (x + CharSize * 2 >= EngineWindow.Instance.ClientSize.X)
            {
                x = 0;
                float dir = flowDown ? 1 : -1;
                y += dir * (CharSize * Aspect + LineSpacing);
                if (c == '\n')
                    continue;

            }//32 - 126
            int charCode = c;
            //Check if the character is not supported, if not, print the fallback character
            if (charCode < 32 || charCode > 126)
                charCode = 127;
            charCode -= ' ';
            //calculate letter dimensions
            var letterPos = new Vector2(10 + x, 10 + y);
            var letterScale = new Vector2(CharSize, CharSize * Aspect);

            var r = new Renderer(fontShader, quad);

            //calculate font atlas position of the letter
            float charX = charCode % NumChars, charY = charCode / NumChars;

            charX *= OffsetX;
            charY *= OffsetY;


            //Compute the uv rect
            float uMin = charX / AtlasSize + ScaleX;
            float vMin = charY / AtlasSize + ScaleY;
            float uMax = (charX + OffsetX) / AtlasSize - ScaleX;
            float vMax = (charY + OffsetY) / AtlasSize - ScaleY;

            r.material = new FontShaderMaterial()
            {
                uvRect = (uMin, vMin, uMax, vMax),
                texture = ResourceManager.Textures[ResourceIndex.Textures.FontAtlas],
                model = Matrix4.CreateScale(letterScale.X, letterScale.Y, 0) * Matrix4.CreateTranslation(letterPos.X, letterPos.Y, -5),
                colour = Colour

            };
            yield return r;
            x += CharSize;
        }
        parts.Clear();
    }

}