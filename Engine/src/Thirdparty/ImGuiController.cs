using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Terr3D.Client.Utils;

public unsafe class ImGuiController : IDisposable
{
    private bool _framePeristed;
    private int _fontTexture;
    private int _shader;
    private int _shaderFontLocation;
    private int _vbo;
    private int _vao;
    private int _ebo;
    private int _windowWidth;
    private int _windowHeight;

    public ImGuiController(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;

        int context = (int)ImGui.GetCurrentContext();
        if (context == 0)
        {
            ImGui.CreateContext();
        }

        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        CreateDeviceResources();

        io.DisplaySize = new System.Numerics.Vector2(_windowWidth, _windowHeight);
        io.DisplayFramebufferScale = System.Numerics.Vector2.One;
    }

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public void Update(GameWindow wnd, float deltaSeconds)
    {
        if (_framePeristed)
        {
            ImGui.Render();
            _framePeristed = false;
        }

        var io = ImGui.GetIO();
        io.DeltaTime = deltaSeconds;

        // Dynamically calculate the DPI scale factor based on the window's physical vs logical size
        io.DisplaySize = new System.Numerics.Vector2(wnd.ClientSize.X, wnd.ClientSize.Y);
        if (wnd.ClientSize.X > 0 && wnd.ClientSize.Y > 0)
        {
            io.DisplayFramebufferScale = new System.Numerics.Vector2(
                (float)wnd.FramebufferSize.X / wnd.ClientSize.X,
                (float)wnd.FramebufferSize.Y / wnd.ClientSize.Y);
        }

        // Use wnd.MousePosition for floating-point precision coordinates rather than the integer MouseState
        io.AddMousePosEvent(wnd.MousePosition.X, wnd.MousePosition.Y);

        var mouseState = wnd.MouseState;
        io.AddMouseButtonEvent(0, mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left));
        io.AddMouseButtonEvent(1, mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right));
        io.AddMouseButtonEvent(2, mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle));

        var keyboardState = wnd.KeyboardState;
        io.AddKeyEvent(ImGuiKey.ModCtrl, keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl));
        io.AddKeyEvent(ImGuiKey.ModShift, keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift));
        io.AddKeyEvent(ImGuiKey.ModAlt, keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt));
        io.AddKeyEvent(ImGuiKey.ModSuper, keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper));

        ImGui.NewFrame();
        _framePeristed = true;
    }

    public void PressChar(char keyChar)
    {
        ImGui.GetIO().AddInputCharacter(keyChar);
    }

    public void MouseScroll(Vector2 offset)
    {
        ImGui.GetIO().AddMouseWheelEvent(offset.X, offset.Y);
    }

    private ImGuiKey TranslateKey(Keys key)
    {
        if (key >= Keys.D0 && key <= Keys.D9)
            return key - Keys.D0 + ImGuiKey._0;

        if (key >= Keys.A && key <= Keys.Z)
            return key - Keys.A + ImGuiKey.A;

        if (key >= Keys.KeyPad0 && key <= Keys.KeyPad9)
            return key - Keys.KeyPad0 + ImGuiKey.Keypad0;

        if (key >= Keys.F1 && key <= Keys.F24)
            return key - Keys.F1 + ImGuiKey.F24;

        switch (key)
        {
            case Keys.Tab: return ImGuiKey.Tab;
            case Keys.Left: return ImGuiKey.LeftArrow;
            case Keys.Right: return ImGuiKey.RightArrow;
            case Keys.Up: return ImGuiKey.UpArrow;
            case Keys.Down: return ImGuiKey.DownArrow;
            case Keys.PageUp: return ImGuiKey.PageUp;
            case Keys.PageDown: return ImGuiKey.PageDown;
            case Keys.Home: return ImGuiKey.Home;
            case Keys.End: return ImGuiKey.End;
            case Keys.Insert: return ImGuiKey.Insert;
            case Keys.Delete: return ImGuiKey.Delete;
            case Keys.Backspace: return ImGuiKey.Backspace;
            case Keys.Space: return ImGuiKey.Space;
            case Keys.Enter: return ImGuiKey.Enter;
            case Keys.Escape: return ImGuiKey.Escape;
            case Keys.Apostrophe: return ImGuiKey.Apostrophe;
            case Keys.Comma: return ImGuiKey.Comma;
            case Keys.Minus: return ImGuiKey.Minus;
            case Keys.Period: return ImGuiKey.Period;
            case Keys.Slash: return ImGuiKey.Slash;
            case Keys.Semicolon: return ImGuiKey.Semicolon;
            case Keys.Equal: return ImGuiKey.Equal;
            case Keys.LeftBracket: return ImGuiKey.LeftBracket;
            case Keys.Backslash: return ImGuiKey.Backslash;
            case Keys.RightBracket: return ImGuiKey.RightBracket;
            case Keys.GraveAccent: return ImGuiKey.GraveAccent;
            case Keys.CapsLock: return ImGuiKey.CapsLock;
            case Keys.ScrollLock: return ImGuiKey.ScrollLock;
            case Keys.NumLock: return ImGuiKey.NumLock;
            case Keys.PrintScreen: return ImGuiKey.PrintScreen;
            case Keys.Pause: return ImGuiKey.Pause;
            case Keys.KeyPadDecimal: return ImGuiKey.KeypadDecimal;
            case Keys.KeyPadDivide: return ImGuiKey.KeypadDivide;
            case Keys.KeyPadMultiply: return ImGuiKey.KeypadMultiply;
            case Keys.KeyPadSubtract: return ImGuiKey.KeypadSubtract;
            case Keys.KeyPadAdd: return ImGuiKey.KeypadAdd;
            case Keys.KeyPadEnter: return ImGuiKey.KeypadEnter;
            case Keys.KeyPadEqual: return ImGuiKey.KeypadEqual;
            default: return ImGuiKey.None;
        }
    }

    public void AddKeyEvent(Keys key, bool down)
    {
        var io = ImGui.GetIO();
        ImGuiKey imKey = TranslateKey(key);

        if (imKey != ImGuiKey.None)
        {
            io.AddKeyEvent(imKey, down);
        }
    }

    public void Render()
    {
        if (_framePeristed)
        {
            ImGui.Render();
            _framePeristed = false;
        }
        RenderDrawData(ImGui.GetDrawData());
    }

    private void RenderDrawData(ImDrawDataPtr draw_data)
    {
        if (draw_data.CmdListsCount == 0) return;

        var io = ImGui.GetIO();

        // Calculate the actual physical pixel dimensions
        int fbWidth = (int)(draw_data.DisplaySize.X * draw_data.FramebufferScale.X);
        int fbHeight = (int)(draw_data.DisplaySize.Y * draw_data.FramebufferScale.Y);
        if (fbWidth <= 0 || fbHeight <= 0) return;

        GL.GetInteger(GetPName.CurrentProgram, out int prevProgram);
        GL.GetInteger(GetPName.TextureBinding2D, out int prevTexture);
        GL.GetInteger(GetPName.ArrayBufferBinding, out int prevArrayBuffer);
        GL.GetInteger(GetPName.VertexArrayBinding, out int prevVertexArray);

        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ScissorTest);

        // Bind viewport to the physical framebuffer size, not the logical window size
        GL.Viewport(0, 0, fbWidth, fbHeight);

        float L = draw_data.DisplayPos.X;
        float R = draw_data.DisplayPos.X + draw_data.DisplaySize.X;
        float T = draw_data.DisplayPos.Y;
        float B = draw_data.DisplayPos.Y + draw_data.DisplaySize.Y;

        Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(L, R, B, T, -1.0f, 1.0f);

        GL.UseProgram(_shader);
        GL.Uniform1(_shaderFontLocation, 0);
        GL.UniformMatrix4(GL.GetUniformLocation(_shader, "ProjectionMatrix"), false, ref mvp);
        GL.BindVertexArray(_vao);

        draw_data.ScaleClipRects(io.DisplayFramebufferScale);

        for (int n = 0; n < draw_data.CmdListsCount; n++)
        {
            var cmd_list = draw_data.CmdLists[n];

            int vtx_size = cmd_list.VtxBuffer.Size * sizeof(ImDrawVert);
            int idx_size = cmd_list.IdxBuffer.Size * sizeof(ushort);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vtx_size, cmd_list.VtxBuffer.Data, BufferUsageHint.StreamDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, idx_size, cmd_list.IdxBuffer.Data, BufferUsageHint.StreamDraw);

            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
            {
                var pcmd = cmd_list.CmdBuffer[cmd_i];

                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);

                    var clip = pcmd.ClipRect;

                    // Map logical clip coordinates to physical screen pixels for the scissor test
                    clip.X = (clip.X - draw_data.DisplayPos.X) * draw_data.FramebufferScale.X;
                    clip.Y = (clip.Y - draw_data.DisplayPos.Y) * draw_data.FramebufferScale.Y;
                    clip.Z = (clip.Z - draw_data.DisplayPos.X) * draw_data.FramebufferScale.X;
                    clip.W = (clip.W - draw_data.DisplayPos.Y) * draw_data.FramebufferScale.Y;

                    GL.Scissor((int)clip.X, (int)(fbHeight - clip.W), (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));

                    if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                    {
                        GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), (int)pcmd.VtxOffset);
                    }
                    else
                    {
                        GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)(pcmd.IdxOffset * sizeof(ushort)));
                    }
                }
            }
        }

        GL.UseProgram(prevProgram);
        GL.BindTexture(TextureTarget.Texture2D, prevTexture);
        GL.BindVertexArray(prevVertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
        GL.Disable(EnableCap.ScissorTest);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }

    private void CreateDeviceResources()
    {
        string vertexShaderSource = @"#version 330 core
uniform mat4 ProjectionMatrix;
layout (location = 0) in vec2 in_position;
layout (location = 1) in vec2 in_texCoord;
layout (location = 2) in vec4 in_color;
out vec2 frag_texCoord;
out vec4 frag_color;
void main()
{
    frag_texCoord = in_texCoord;
    frag_color = in_color;
    gl_Position = ProjectionMatrix * vec4(in_position, 0, 1);
}";

        string fragmentShaderSource = @"#version 330 core
uniform sampler2D in_font;
in vec2 frag_texCoord;
in vec4 frag_color;
out vec4 out_color;
void main()
{
    out_color = frag_color * texture(in_font, frag_texCoord);
}";

        _shader = GL.CreateProgram();
        int vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, vertexShaderSource);
        GL.CompileShader(vs);

        int fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, fragmentShaderSource);
        GL.CompileShader(fs);

        GL.AttachShader(_shader, vs);
        GL.AttachShader(_shader, fs);
        GL.LinkProgram(_shader);

        GL.DetachShader(_shader, vs);
        GL.DetachShader(_shader, fs);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);

        _shaderFontLocation = GL.GetUniformLocation(_shader, "in_font");

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(ImDrawVert), 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(ImDrawVert), 8);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, sizeof(ImDrawVert), 16);

        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height);

        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        io.Fonts.SetTexID(_fontTexture);
        io.Fonts.ClearTexData();

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }

    public void Dispose()
    {
        GL.DeleteProgram(_shader);
        GL.DeleteTexture(_fontTexture);
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        GL.DeleteVertexArray(_vao);
    }
}