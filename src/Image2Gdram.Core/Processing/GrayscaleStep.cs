using Image2Gdram.Core.Imaging;

namespace Image2Gdram.Core.Processing;

/// <summary>
/// Шаг 4: <c>Y = (299·R + 587·G + 114·B + 500) / 1000</c> (решение F-09).
/// Альфа и порядок байтов упаковки не участвуют: на вход приходит уже непрозрачный RGBA.
/// </summary>
public static class GrayscaleStep
{
    public static byte Luminance(byte r, byte g, byte b) =>
        (byte)((299 * r + 587 * g + 114 * b + 500) / 1000);

    public static GrayImage Apply(RgbaImage source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var pixels = new byte[source.Width * source.Height];
        ReadOnlySpan<byte> src = source.Pixels;
        for (int p = 0, i = 0; p < pixels.Length; p++, i += RgbaImage.BytesPerPixel)
        {
            pixels[p] = Luminance(src[i], src[i + 1], src[i + 2]);
        }

        return new GrayImage(source.Width, source.Height, pixels);
    }
}
