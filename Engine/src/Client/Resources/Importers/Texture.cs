using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Terr3D.Client.Resources.Importers;

public class TextureImporter : Importer<Texture>
{
    protected override Texture Import(Stream data, string path)
    {
        using Image<Rgba32> image = Image.Load<Rgba32>(data);
        return new(image);
    }
}