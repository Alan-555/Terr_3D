using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.Modules;
using Terr3D.Server.World;
using Terr3D.Utils;

namespace Terr3D.Server.Entities;

/// <summary>
/// The canvas entity provides logic to render text to the screen
/// </summary>
public class Canvas : Entity, ICanvasProvider
{
    public FontRenderer FontRenderer { get; private set; }

    private string ConsoleText
    {
        get => FontRenderer.Console;
        set => FontRenderer.Console = value;
    }

    private bool ConsoleActive
    {
        get => _consoleActive;
        set
        {
            _consoleActive = value;
            FontRenderer.DoShowConsole = _consoleActive;
        }
    }

    Canvas ICanvasProvider.Canvas => this;

    private bool _consoleActive = false;

    private readonly List<string> _history = new();
    private int _historyScrollIndex = -1;
    private bool _isDisplayingOutput = false;
    private readonly DebugConsole _debugConsole;

    public Canvas(Scene scene, string name) : base(scene, name, true)
    {
        Register();
        FontRenderer = AddComponent<FontRenderer>();
        _debugConsole = new DebugConsole(scene);

        EngineWindow.Instance.TextInput += OnTextInput;
        EngineWindow.Instance.KeyDown += OnKeyDown;
    }
    
    public void Register()
    {
        Onstage.Globals.Register<ICanvasProvider>(this);
    }

    public override void OnDestroyed()
    {
        EngineWindow.Instance.TextInput -= OnTextInput;
        EngineWindow.Instance.KeyDown -= OnKeyDown;
    }

    public void RenderLabel(string text, int order)
    {

        FontRenderer.RenderLabel(text, order);
    }

    private void OnTextInput(TextInputEventArgs e)
    {
        if (!ConsoleActive) return;

        ResetConsoleStateIfOutputShowing();
        ConsoleText += e.AsString;
    }

    private void OnKeyDown(KeyboardKeyEventArgs e)
    {
        // Handle global toggles
        if (!e.IsRepeat)
        {
            if (e.Key == Keys.F2)
            {
                ConsoleActive = !ConsoleActive;
                return;
            }

            if (e.Key == Keys.F3)
            {
                FontRenderer.DrawDebug = !FontRenderer.DrawDebug;
                return;
            }
        }

        if (!ConsoleActive) return;

        if(e.Key == Keys.Tab)
        {
            var i = ConsoleText.LastIndexOf('\n');
            if(i==-1) return;
            ConsoleText = ConsoleText.Substring(ConsoleText.IndexOf('\n')+1);
            return;
        }

        ResetConsoleStateIfOutputShowing();

        switch (e.Key)
        {
            case Keys.Backspace:
                HandleBackspace();
                break;
            case Keys.Enter:
                HandleEnter();
                break;
            case Keys.Up:
                ScrollHistory(1);
                break;
            case Keys.Down:
                ScrollHistory(-1);
                break;
        }
    }

    private void ResetConsoleStateIfOutputShowing()
    {
        if (_isDisplayingOutput)
        {
            FontRenderer.Colour = Vector3.One;
            ConsoleText = "";
            _isDisplayingOutput = false;
        }
    }

    private void HandleBackspace()
    {
        if (ConsoleText.Length > 0)
        {
            ConsoleText = ConsoleText[..^1];
        }
    }

    private void HandleEnter()
    {
        try
        {
            RunCommand(ConsoleText);
        }
        catch (Exception ex)
        {
            Diagnostics.Error(ex.ToString());
        }
    }

    private void ScrollHistory(int direction)
    {
        if (_history.Count == 0) return;

        _historyScrollIndex += direction;
        _historyScrollIndex = Math.Clamp(_historyScrollIndex, 0, _history.Count - 1);
        ConsoleText = _history[_historyScrollIndex];
    }

    public void OutputToConsole(string str)
    {
        _historyScrollIndex = -1;

        if (str.StartsWith("\\e"))
        {
            FontRenderer.Colour = (1, 0, 0);
            str = str.Replace("\\e", "");
        }

        ConsoleText = str;
        _isDisplayingOutput = true;
    }

    public void RunCommand(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return;

        _history.Insert(0, str);
        _historyScrollIndex = -1;

        str = str.Replace("\\_", " ");

        var parts = str.Split(' ');
        var command = parts[0];
        var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        /*for (int i = 0; i < args.Length; i++)
        {
            args[i] = args[i].Replace("\\_", " ");
        }*/

        Diagnostics.Debug($"Run cmd: {command}");
        var output = _debugConsole.RunCommand(command, args);

        if (output != null)
        {
            OutputToConsole(output);
        }
        else
        {
            ConsoleText = "";
            ConsoleActive = false;
        }
    }
}