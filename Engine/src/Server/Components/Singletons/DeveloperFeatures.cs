using System.Numerics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Entities;
using Terr3D.Server.Modules;
using Terr3D.Server.Core;
using Terr3D.Utils;
using System.Runtime.CompilerServices;
using ImGuiNET;

namespace Terr3D.Server.Components;

public class DeveloperFeatures() : SingletonComponent()
{
    private bool _consoleActive = false;
    private bool _debugOverlayer = false;

    private DebugConsole _debugConsole;

    protected override void OnUpdate(float dt)
    {
        if (_consoleActive)
            DrawConsole("Terr3D Console");
        if(_debugOverlayer)
            DrawOverlayer();
    }

    protected override void OnInitialise()
    {
        base.OnInitialise();
        _debugConsole = new DebugConsole();
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
                _debugOverlayer = !_debugOverlayer;
                return;
            }
        }
    }

    private readonly List<string> _logHistory = new List<string>();
    private readonly List<string> _commandHistory = new List<string>();
    private readonly List<string> _labels = new();
    private string _inputBuffer = "";
    private bool _scrollToBottom = false;
    private bool _autoScroll = true;
    private int _commandHistoryPos = -1;


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
    private void DrawOverlayer()
    {
        ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration
                       | ImGuiWindowFlags.AlwaysAutoResize
                       | ImGuiWindowFlags.NoSavedSettings
                       | ImGuiWindowFlags.NoFocusOnAppearing
                       | ImGuiWindowFlags.NoNav
                       | ImGuiWindowFlags.NoMove;

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10), ImGuiCond.Always);

        ImGui.SetNextWindowBgAlpha(0.0f);

        if (ImGui.Begin("Debug Overlay", flags))
        {
            ImGui.TextColored(new System.Numerics.Vector4(1.0f, 1.0f, 0.0f, 1.0f), "Engine Debug Stats");
            foreach(var line in _labels)
            {
                ImGui.Text(line);
            }
            _labels.Clear();
            ImGui.Text($"Camera Pos: {World.ActiveCamera?.Transform}");
        }
        ImGui.End();
    }

    private void DrawConsole(string windowTitle)
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

        ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackHistory;

        ImGui.SetNextItemWidth(-1.0f);
        unsafe
        {
            if (ImGui.InputText("##ConsoleInput", ref _inputBuffer, 256, inputFlags, HistoryCallback))
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
        if (str.Trim() == "clear") ClearLog();

        str = str.Replace("\\_", " ");

        var parts = str.Split(' ');
        var command = parts[0];
        var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        /*for (int i = 0; i < args.Length; i++)
        {
            args[i] = args[i].Replace("\\_", " ");
        }*/

        Diagnostics.Debug($"Run cmd: {command}");
        if (_commandHistory.Count == 0 || _commandHistory[_commandHistory.Count - 1] != command)
        {
            _commandHistory.Add(str);
        }
        _commandHistoryPos = -1;
        var output = _debugConsole.RunCommand(command, args);

        if (output != null)
            AddLog(output);

    }

    private unsafe int HistoryCallback(ImGuiInputTextCallbackData* data)
    {
        ImGuiInputTextCallbackDataPtr dataPtr = new ImGuiInputTextCallbackDataPtr(data);

        if (dataPtr.EventFlag == ImGuiInputTextFlags.CallbackHistory)
        {
            int prevHistoryPos = _commandHistoryPos;

            if (dataPtr.EventKey == ImGuiKey.UpArrow)
            {
                if (_commandHistoryPos == -1)
                    _commandHistoryPos = _commandHistory.Count - 1;
                else if (_commandHistoryPos > 0)
                    _commandHistoryPos--;
            }
            else if (dataPtr.EventKey == ImGuiKey.DownArrow)
            {
                if (_commandHistoryPos != -1)
                {
                    if (++_commandHistoryPos >= _commandHistory.Count)
                        _commandHistoryPos = -1;
                }
            }

            if (prevHistoryPos != _commandHistoryPos)
            {
                string historyStr = (_commandHistoryPos >= 0) ? _commandHistory[_commandHistoryPos] : "";

                dataPtr.DeleteChars(0, dataPtr.BufTextLen);
                dataPtr.InsertChars(0, historyStr);
            }
        }
        return 0;
    }


    public static void Log(string message) => World.GetSingletonOrNull<DeveloperFeatures>()?.AddLog(message);
    public static void DebugLabel(string label) => World.GetSingletonOrNull<DeveloperFeatures>()?._labels.Add(label);
}