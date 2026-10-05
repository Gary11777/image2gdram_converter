using Image2Gdram.Core.Imaging;

namespace Image2Gdram.Core.Processing;

/// <summary>
/// Шаг 2: сначала поворот по часовой стрелке, затем отражение (решения F-07, N-10).
/// 90° и 270° меняют местами ширину и высоту. Преобразование без потерь.
/// </summary>
public static class RotateFlipStep
{
    public static RgbaImage Apply(RgbaImage source, Rotation rotation, bool flipHorizontal, bool flipVertical)
    {
        ArgumentNullException.ThrowIfNull(source);
        RgbaImage rotated = Rotate(source, rotation);
        if (!flipHorizontal && !flipVertical)
        {
            return rotated;
        }

        return Flip(rotated, flipHorizontal, flipVertical);
    }

    public static RgbaImage Rotate(RgbaImage source, Rotation rotation)
    {
        ArgumentNullException.ThrowIfNull(source);
        return rotation switch
        {
            Rotation.Rotate0 => source.Clone(),
            Rotation.Rotate90 => Map(source, source.Height, source.Width, (x, y, w, h) => (y, h - 1 - x)),
            Rotation.Rotate180 => Map(source, source.Width, source.Height, (x, y, w, h) => (w - 1 - x, h - 1 - y)),
            Rotation.Rotate270 => Map(source, source.Height, source.Width, (x, y, w, h) => (w - 1 - y, x)),
            _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "Unknown rotation."),
        };
    }

    public static RgbaImage Flip(RgbaImage source, bool horizontal, bool vertical)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!horizontal && !vertical)
        {
            return source.Clone();
        }

        int w = source.Width;
        int h = source.Height;
        return Map(source, w, h, (x, y, _, _) => (horizontal ? w - 1 - x : x, vertical ? h - 1 - y : y));
    }

    private static RgbaImage Map(RgbaImage source, int dstWidth, int dstHeight, Coord map)
    {
        var destination = new RgbaImage(dstWidth, dstHeight);
        byte[] dst = destination.PixelBuffer;
        for (int y = 0; y < dstHeight; y++)
        {
            for (int x = 0; x < dstWidth; x++)
            {
                (int sx, int sy) = map(x, y, source.Width, source.Height);
                source.GetPixel(sx, sy, out byte r, out byte g, out byte b, out byte a);
                int i = (y * dstWidth + x) * RgbaImage.BytesPerPixel;
                dst[i] = r;
                dst[i + 1] = g;
                dst[i + 2] = b;
                dst[i + 3] = a;
            }
        }

        return destination;
    }

    private delegate (int X, int Y) Coord(int x, int y, int sourceWidth, int sourceHeight);
}
