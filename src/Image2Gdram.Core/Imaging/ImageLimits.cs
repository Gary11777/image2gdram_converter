namespace Image2Gdram.Core.Imaging;

/// <summary>Пределы загрузки (п. 4.1.1 ТЗ, решение N-20).</summary>
public static class ImageLimits
{
    /// <summary>Максимальная сторона исходного изображения, включительно.</summary>
    public const int MaxSide = 8192;

    /// <summary>Суммарный объём декодированных кадров в байтах RGBA. Больше этого значения загрузка не начинает.</summary>
    public const long MaxRgbaBytes = 512L * 1024 * 1024;

    public static bool SideExceedsLimit(int width, int height) => width > MaxSide || height > MaxSide;

    public static bool MemoryExceedsLimit(int width, int height, int frameCount)
    {
        if (frameCount < 1 || width < 1 || height < 1)
        {
            return false;
        }

        try
        {
            long bytes = checked((long)width * height * RgbaImage.BytesPerPixel * frameCount);
            return bytes > MaxRgbaBytes;
        }
        catch (OverflowException)
        {
            return true;
        }
    }
}
