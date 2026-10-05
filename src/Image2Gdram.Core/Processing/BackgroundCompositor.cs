using Image2Gdram.Core.Imaging;

namespace Image2Gdram.Core.Processing;

/// <summary>
/// Шаг 1: наложение на фон, <c>c' = (c·a + bg·(255 − a) + 127) / 255</c> (решение F-06).
/// Результат непрозрачен.
/// </summary>
public static class BackgroundCompositor
{
    public static byte Channel(byte color, byte alpha, byte background) =>
        (byte)((color * alpha + background * (255 - alpha) + 127) / 255);

    public static RgbaImage Apply(RgbaImage source, BackgroundColor background)
    {
        ArgumentNullException.ThrowIfNull(source);
        byte bg = BackgroundValue(background);
        var result = new RgbaImage(source.Width, source.Height);
        ReadOnlySpan<byte> src = source.Pixels;
        byte[] dst = result.PixelBuffer;
        for (int i = 0; i < src.Length; i += RgbaImage.BytesPerPixel)
        {
            byte a = src[i + 3];
            dst[i] = Channel(src[i], a, bg);
            dst[i + 1] = Channel(src[i + 1], a, bg);
            dst[i + 2] = Channel(src[i + 2], a, bg);
            dst[i + 3] = 255;
        }

        return result;
    }

    public static byte BackgroundValue(BackgroundColor background) => background switch
    {
        BackgroundColor.White => 255,
        BackgroundColor.Black => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(background), background, "Unknown background."),
    };
}
