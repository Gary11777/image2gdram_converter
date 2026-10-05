using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Processing;
using Image2Gdram.TestAssets;

namespace Image2Gdram.Core.Tests.Processing;

internal static class ProcessingTestImages
{
    public static RgbaImage Solid(int width, int height, byte r, byte g, byte b, byte a = 255) =>
        new(width, height, r, g, b, a);

    public static RgbaImage FromPattern(PatternBitmap pattern)
    {
        var image = new RgbaImage(pattern.Width, pattern.Height, 255, 255, 255, 255);
        for (int y = 0; y < pattern.Height; y++)
        {
            for (int x = 0; x < pattern.Width; x++)
            {
                if (pattern.IsActive(x, y))
                {
                    image.SetPixel(x, y, 0, 0, 0, 255);
                }
            }
        }

        return image;
    }

    public static int CountActive(MonoBitmap bitmap)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap[x, y])
                {
                    count++;
                }
            }
        }

        return count;
    }

    public static GrayImage Flat(int width, int height, byte luminance)
    {
        var pixels = new byte[width * height];
        Array.Fill(pixels, luminance);
        return new GrayImage(width, height, pixels);
    }
}
