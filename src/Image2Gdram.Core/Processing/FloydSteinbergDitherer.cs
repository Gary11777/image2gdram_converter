using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Processing;

/// <summary>
/// Дизеринг Флойда — Стейнберга (решение F-10): обход слева направо, без «змейки».
/// Ошибка <c>v − out</c> распределяется 7/16, 3/16, 5/16, 1/16. За границей отбрасывается.
/// </summary>
public sealed class FloydSteinbergDitherer : IBinarizer
{
    public MonoBitmap Apply(GrayImage gray, int threshold)
    {
        ArgumentNullException.ThrowIfNull(gray);
        ThresholdBinarizer.EnsureThreshold(threshold);
        int width = gray.Width;
        int height = gray.Height;
        double[] buffer = ToDouble(gray);
        var bitmap = new MonoBitmap(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = buffer[y * width + x];
                bool active = value < threshold;
                double error = value - (active ? 0 : 255);
                bitmap[x, y] = active;
                Add(buffer, width, height, x + 1, y, error * 7 / 16);
                Add(buffer, width, height, x - 1, y + 1, error * 3 / 16);
                Add(buffer, width, height, x, y + 1, error * 5 / 16);
                Add(buffer, width, height, x + 1, y + 1, error * 1 / 16);
            }
        }

        return bitmap;
    }

    internal static double[] ToDouble(GrayImage gray)
    {
        ReadOnlySpan<byte> pixels = gray.Pixels;
        var buffer = new double[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            buffer[i] = pixels[i];
        }

        return buffer;
    }

    internal static void Add(double[] buffer, int width, int height, int x, int y, double error)
    {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height)
        {
            return;
        }

        buffer[y * width + x] += error;
    }
}
