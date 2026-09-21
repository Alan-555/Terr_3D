using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Terr3D.Client.Resources;

/// <summary>
/// Loads a texture and then uploads it to the GPU
/// </summary>
public class Texture : GPU_Resource
{
    public Texture(string path)
    {
        //Load the texture from the disk
        using Image<Rgba32> image = Image.Load<Rgba32>(path);

        //Create a texture in the VRAM
        _resHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, this);

        //Copy the image pixel data over to the GPU
        image.ProcessPixelRows(accessor =>
        {
            byte[] pixels = new byte[image.Width * image.Height * 4];
            for (int y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                System.Runtime.InteropServices.MemoryMarshal.AsBytes(row).CopyTo(pixels.AsSpan(y * image.Width * 4));
            }

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          image.Width, image.Height, 0,
                          PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        });
        
        //Texture filtering
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
    }

    public Texture(int existingTexture)
    {
        _resHandle = existingTexture;
    }

    public override void Dispose()
    {
        if (_resHandle != 0)
        {
            GL.DeleteTexture(_resHandle);
        }
        base.Dispose();
    }
}