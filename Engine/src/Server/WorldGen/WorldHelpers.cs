using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Terr3D.Server.WorldGen;

public static class WorldHelpers
{

    /// <summary>
    /// Uses gaussian blur to interpolate terrain height. Mostly experimental, does not work well
    /// </summary>
    public static float SampleGaussianHeight(Image<Rgba32> image, float x, float y)
    {
        int[,] kernel = {
            { 1,  4,  6,  4,  1 },
            { 4, 16, 24, 16,  4 },
            { 6, 24, 36, 24,  6 },
            { 4, 16, 24, 16,  4 },
            { 1,  4,  6,  4,  1 }
        };

        float weightSum = 256f;

        //Nearest neighbour
        int cx = (int)Math.Round(x);
        int cy = (int)Math.Round(y);

        float blendedHeight = 0f;

        for (int ky = -2; ky <= 2; ky++)
        {
            for (int kx = -2; kx <= 2; kx++)
            {
                //Clamp to prevent reading OOB
                int sampleX = Math.Clamp(cx + kx, 0, image.Width - 1);
                int sampleY = Math.Clamp(cy + ky, 0, image.Height - 1);

                //We care about the red channel only
                float pixelHeight = image[sampleX, sampleY].R;

                //Apply the kernel
                int weight = kernel[ky + 2, kx + 2];
                blendedHeight += pixelHeight * weight;
            }
        }

        //Average out the result and normalise
        return (blendedHeight / weightSum) / 255f;
    }

    /// <summary>
    /// Samples terrain height and uses bilinear interpolation
    /// </summary>
    public static float SampleBilinearHeight(Image<Rgba32> image, float x, float y)
    {

        x = Math.Clamp(x, 0, image.Width - 1);
        y = Math.Clamp(y, 0, image.Height - 1);

        int x1 = (int)Math.Floor(x);
        int y1 = (int)Math.Floor(y);

        int x2 = Math.Min(x1 + 1, image.Width - 1);
        int y2 = Math.Min(y1 + 1, image.Height - 1);

        float fx = x - x1;
        float fy = y - y1;

        float r00 = image[x1, y1].R; // Top-Left
        float r10 = image[x2, y1].R; // Top-Right
        float r01 = image[x1, y2].R; // Bottom-Left
        float r11 = image[x2, y2].R; // Bottom-Right

        float inverseFx = 1.0f - fx;
        float inverseFy = 1.0f - fy;

        float w00 = inverseFx * inverseFy;
        float w10 = fx * inverseFy;
        float w01 = inverseFx * fy;
        float w11 = fx * fy;

        float blendedHeight = r00 * w00 + r10 * w10 + r01 * w01 + r11 * w11;

        return blendedHeight / 255f;
    }

    
}