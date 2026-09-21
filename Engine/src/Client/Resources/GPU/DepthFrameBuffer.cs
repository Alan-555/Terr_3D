using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Terr3D.Utils;

namespace Terr3D.Client.Resources;


public class DepthFrameBuffer : GPU_Resource
{
    public readonly int depthMap;
    public DepthFrameBuffer(int res)
    {
        _resHandle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _resHandle);

        depthMap = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, depthMap);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent,
                      res, res, 0,
                      PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthMap, 0);

        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal); 
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.None);

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            Diagnostics.Error($"Framebuffer is incomplete: {status}");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }


    public override void Dispose()
    {
        if (_resHandle != 0)
        {
            GL.DeleteFramebuffer(_resHandle);
            GL.DeleteTexture(depthMap);
        }
        base.Dispose();
    }

}