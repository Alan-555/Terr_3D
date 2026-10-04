using OpenTK.Graphics.OpenGL4;
using Terr3D.Utils;

namespace Terr3D.Client.Resources;

public class ColourFrameBuffer : GPU_Object
{
    public readonly int colourTexture;
    public int Width { get; private set; }
    public int Height { get; private set; }

    public ColourFrameBuffer(int width, int height) : base(GL.GenFramebuffer())
    {
        Width = width;
        Height = height;
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, this);

        colourTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, colourTexture);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb,
                      width, height, 0,
                      PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colourTexture, 0);

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            Diagnostics.Error($"Colour Framebuffer is incomplete: {status}");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    protected override void CollectObject()
    {
        GL.DeleteFramebuffer(this);
        GL.DeleteTexture(colourTexture);

    }
}
