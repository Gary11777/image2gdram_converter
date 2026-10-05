using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Processing;

/// <summary>
/// Упорядоченный дизеринг Байера 4×4 или 8×8 (решение F-10).
/// <c>active = Y &lt; threshold + 255·((M + 0,5) / n² − 0,5)</c>.
/// </summary>
public sealed class BayerDitherer : IBinarizer
{
    private readonly int _size;
    private readonly int[,] _matrix;

    public BayerDitherer(int size)
    {
        if (size is not (4 or 8))
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Bayer matrix size must be 4 or 8.");
        }

        _size = size;
        _matrix = Matrix(size);
    }

    public MonoBitmap Apply(GrayImage gray, int threshold)
    {
        ArgumentNullException.ThrowIfNull(gray);
        ThresholdBinarizer.EnsureThreshold(threshold);
        int n = _size;
        int n2 = n * n;
        var bitmap = new MonoBitmap(gray.Width, gray.Height);
        ReadOnlySpan<byte> pixels = gray.Pixels;
        for (int y = 0; y < gray.Height; y++)
        {
            int row = y * gray.Width;
            int my = y % n;
            for (int x = 0; x < gray.Width; x++)
            {
                double level = threshold + 255.0 * ((_matrix[my, x % n] + 0.5) / n2 - 0.5);
                bitmap[x, y] = pixels[row + x] < level;
            }
        }

        return bitmap;
    }

    /// <summary>
    /// Индексная матрица. 4×4 совпадает с таблицей F-10, 8×8 — то же рекуррентное правило.
    /// База 2×2: <c>0 2 / 3 1</c>, далее <c>[[4M, 4M+2], [4M+3, 4M+1]]</c>.
    /// </summary>
    public static int[,] Matrix(int size)
    {
        if (size is not (4 or 8))
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Bayer matrix size must be 4 or 8.");
        }

        int[,] current =
        {
            { 0, 2 },
            { 3, 1 },
        };
        while (current.GetLength(0) < size)
        {
            current = Expand(current);
        }

        return current;
    }

    private static int[,] Expand(int[,] source)
    {
        int n = source.GetLength(0);
        var result = new int[n * 2, n * 2];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                int value = source[y, x] * 4;
                result[y, x] = value;
                result[y, x + n] = value + 2;
                result[y + n, x] = value + 3;
                result[y + n, x + n] = value + 1;
            }
        }

        return result;
    }
}
