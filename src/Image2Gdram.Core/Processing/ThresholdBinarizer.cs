using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Processing;

/// <summary>Пороговый режим: активен при <c>Y &lt; threshold</c> (решение F-09).</summary>
public sealed class ThresholdBinarizer : IBinarizer
{
    public MonoBitmap Apply(GrayImage gray, int threshold)
    {
        ArgumentNullException.ThrowIfNull(gray);
        EnsureThreshold(threshold);
        var bitmap = new MonoBitmap(gray.Width, gray.Height);
        ReadOnlySpan<byte> pixels = gray.Pixels;
        for (int y = 0; y < gray.Height; y++)
        {
            int row = y * gray.Width;
            for (int x = 0; x < gray.Width; x++)
            {
                bitmap[x, y] = pixels[row + x] < threshold;
            }
        }

        return bitmap;
    }

    internal static void EnsureThreshold(int threshold)
    {
        if (threshold is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Threshold must be in 0..255.");
        }
    }
}
