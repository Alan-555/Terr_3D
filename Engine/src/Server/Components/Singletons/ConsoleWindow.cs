using System.Numerics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Entities;
using Terr3D.Server.Modules;
using Terr3D.Server.Engine;
using Terr3D.Utils;
using System.Runtime.CompilerServices;
using ImGuiNET;

namespace Terr3D.Server.Components;

public class ConsoleWindow() : SingletonComponent()
{
    private bool _consoleActive = false;

    private DebugConsole _debugConsole;

    protected override void OnUpdate(float dt)
    {
        if (_consoleActive)
            Draw("Terr3D Console");
    }

    protected override void OnInitialise()
    {
        base.OnInitialise();
        _debugConsole = new DebugConsole(Onstage);
        AddLog("Welcome to Terr3D console. Type list to list all commands");
        EngineWindow.Instance.KeyDown += OnKeyDown;
    }


    protected override void OnDestroyed()
    {
        EngineWindow.Instance.KeyDown -= OnKeyDown;
    }


    private void OnKeyDown(KeyboardKeyEventArgs e)
    {
        // Handle global toggles
        if (!e.IsRepeat)
        {
            if (e.Key == Keys.F2)
            {
                _consoleActive = !_consoleActive;
                EngineWindow.Instance.IsPresentUI = _consoleActive;
                return;
            }

            if (e.Key == Keys.F3)
            {
                //FontRenderer.DrawDebug = !FontRenderer.DrawDebug;
                return;
            }
        }
    }

    private readonly List<string> _logHistory = new List<string>();
    private string _inputBuffer = "";
    private bool _scrollToBottom = false;
    private bool _autoScroll = true;


    public void AddLog(string message)
    {
        _logHistory.Add(message);
        if (_autoScroll)
        {
            _scrollToBottom = true;
        }
    }

    public void ClearLog()
    {
        _logHistory.Clear();
    }

    private void Draw(string windowTitle)
    {
        ImGui.SetNextWindowSize(new Vector2(520, 600), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin(windowTitle))
        {
            ImGui.End();
            return;
        }

        if (ImGui.Button("Clear")) ClearLog();
        ImGui.SameLine();
        ImGui.Checkbox("Auto-scroll", ref _autoScroll);
        ImGui.Separator();

        float footerHeight = ImGui.GetStyle().ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing();

        ImGui.BeginChild("ScrollingRegion", new Vector2(0, -footerHeight), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

        foreach (string item in _logHistory)
        {
            // TextUnformatted is much faster than Text() for large amounts of raw string data
            ImGui.TextUnformatted(item);
        }

        if (_scrollToBottom)
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollToBottom = false;
        }

        ImGui.EndChild();
        ImGui.Separator();

        bool reclaimFocus = false;

        ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.EnterReturnsTrue;

        ImGui.SetNextItemWidth(-1.0f);

        if (ImGui.InputText("##ConsoleInput", ref _inputBuffer, 256, inputFlags))
        {
            string command = _inputBuffer.Trim();
            if (!string.IsNullOrEmpty(command))
            {
                AddLog($"] {command}");
                ExecuteCommand(command);
            }

            _inputBuffer = "";
            reclaimFocus = true;
        }

        ImGui.SetItemDefaultFocus();
        if (reclaimFocus)
        {
            ImGui.SetKeyboardFocusHere(-1);
        }

        ImGui.End();
    }


    public void ExecuteCommand(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return;


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
            AddLog(output);

    }
}