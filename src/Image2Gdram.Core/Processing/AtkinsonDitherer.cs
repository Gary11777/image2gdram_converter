using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Processing;

/// <summary>
/// Дизеринг Аткинсона (решение F-10): по 1/8 ошибки на шесть соседей, 2/8 теряется.
/// Обход слева направо, без «змейки». Значение яркости не ограничивается.
/// </summary>
public sealed class AtkinsonDitherer : IBinarizer
{
    public MonoBitmap Apply(GrayImage gray, int threshold)
    {
        ArgumentNullException.ThrowIfNull(gray);
        ThresholdBinarizer.EnsureThreshold(threshold);
        int width = gray.Width;
        int height = gray.Height;
        double[] buffer = FloydSteinbergDitherer.ToDouble(gray);
        var bitmap = new MonoBitmap(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = buffer[y * width + x];
                bool active = value < threshold;
                double share = (value - (active ? 0 : 255)) / 8;
                bitmap[x, y] = active;
                FloydSteinbergDitherer.Add(buffer, width, height, x + 1, y, share);
                FloydSteinbergDitherer.Add(buffer, width, height, x + 2, y, share);
                FloydSteinbergDitherer.Add(buffer, width, height, x - 1, y + 1, share);
                FloydSteinbergDitherer.Add(buffer, width, height, x, y + 1, share);
                FloydSteinbergDitherer.Add(buffer, width, height, x + 1, y + 1, share);
                FloydSteinbergDitherer.Add(buffer, width, height, x, y + 2, share);
            }
        }

        return bitmap;
    }
}
