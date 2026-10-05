namespace Image2Gdram.Core.Imaging;

/// <summary>
/// Растр RGBA, 8 бит на канал, прямой (не умноженный на альфу) порядок R, G, B, A.
/// Строки сверху вниз, в строке пиксели слева направо.
/// После передачи в <c>ImagePipeline</c> пиксели менять не следует: конвейер кэширует кадр по ссылке.
/// </summary>
public sealed class RgbaImage
{
    public const int BytesPerPixel = 4;

    private readonly byte[] _pixels;

    public RgbaImage(int width, int height)
        : this(width, height, new byte[CheckedLength(width, height)])
    {
    }

    public RgbaImage(int width, int height, byte r, byte g, byte b, byte a)
        : this(width, height)
    {
        Fill(r, g, b, a);
    }

    /// <summary>Забирает массив как есть. Длина должна быть <c>width * height * 4</c>.</summary>
    public RgbaImage(int width, int height, byte[] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        int length = CheckedLength(width, height);
        if (pixels.Length != length)
        {
            throw new ArgumentException($"Pixel buffer must be {length} bytes, got {pixels.Length}.", nameof(pixels));
        }

        Width = width;
        Height = height;
        _pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public ReadOnlySpan<byte> Pixels => _pixels;

    public void GetPixel(int x, int y, out byte r, out byte g, out byte b, out byte a)
    {
        int i = IndexOf(x, y);
        r = _pixels[i];
        g = _pixels[i + 1];
        b = _pixels[i + 2];
        a = _pixels[i + 3];
    }

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
    {
        int i = IndexOf(x, y);
        _pixels[i] = r;
        _pixels[i + 1] = g;
        _pixels[i + 2] = b;
        _pixels[i + 3] = a;
    }

    public void Fill(byte r, byte g, byte b, byte a)
    {
        for (int i = 0; i < _pixels.Length; i += BytesPerPixel)
        {
            _pixels[i] = r;
            _pixels[i + 1] = g;
            _pixels[i + 2] = b;
            _pixels[i + 3] = a;
        }
    }

    public RgbaImage Clone()
    {
        var copy = new byte[_pixels.Length];
        _pixels.CopyTo(copy, 0);
        return new RgbaImage(Width, Height, copy);
    }

    public bool ContentEquals(RgbaImage? other) =>
        other is not null
        && other.Width == Width
        && other.Height == Height
        && _pixels.AsSpan().SequenceEqual(other._pixels);

    internal byte[] PixelBuffer => _pixels;

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

        return (y * Width + x) * BytesPerPixel;
    }

    private static int CheckedLength(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        long length = (long)width * height * BytesPerPixel;
        if (length > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Image is too large for a single buffer.");
        }

        return (int)length;
    }
}
