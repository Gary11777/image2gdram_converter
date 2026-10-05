namespace Image2Gdram.Core.Processing;

/// <summary>Яркость Y, 8 бит, строки сверху вниз (шаг 4).</summary>
public sealed class GrayImage
{
    private readonly byte[] _pixels;

    public GrayImage(int width, int height)
        : this(width, height, new byte[CheckedLength(width, height)])
    {
    }

    public GrayImage(int width, int height, byte[] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        int length = CheckedLength(width, height);
        if (pixels.Length != length)
        {
            throw new ArgumentException($"Luminance buffer must be {length} bytes, got {pixels.Length}.", nameof(pixels));
        }

        Width = width;
        Height = height;
        _pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public byte this[int x, int y]
    {
        get => _pixels[IndexOf(x, y)];
        set => _pixels[IndexOf(x, y)] = value;
    }

    public ReadOnlySpan<byte> Pixels => _pixels;

    private int IndexOf(int x, int y)
    {
        if ((uint)x >= (uint)Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, $"Column must be in 0..{Width - 1}.");
        }

        if ((uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, $"Row must be in 0..{Height - 1}.");
        }

        return y * Width + x;
    }

    private static int CheckedLength(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        long length = (long)width * height;
        if (length > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Image is too large for a single buffer.");
        }

        return (int)length;
    }
}
