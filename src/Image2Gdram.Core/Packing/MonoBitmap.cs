namespace Image2Gdram.Core.Packing;

/// <summary>
/// Монохромный растр: для каждого пикселя — признак «активный» (раздел 3 ТЗ).
/// x — столбец слева направо, y — строка сверху вниз.
/// </summary>
public sealed class MonoBitmap
{
    private readonly bool[] _pixels;

    public MonoBitmap(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        Width = width;
        Height = height;
        _pixels = new bool[checked(width * height)];
    }

    private MonoBitmap(int width, int height, bool[] pixels)
    {
        Width = width;
        Height = height;
        _pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public bool this[int x, int y]
    {
        get => _pixels[IndexOf(x, y)];
        set => _pixels[IndexOf(x, y)] = value;
    }

    /// <summary>Пиксели строки y слева направо.</summary>
    public ReadOnlySpan<bool> GetRow(int y)
    {
        if ((uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, $"Row must be in 0..{Height - 1}.");
        }

        return new ReadOnlySpan<bool>(_pixels, y * Width, Width);
    }

    public MonoBitmap Clone() => new(Width, Height, (bool[])_pixels.Clone());

    /// <summary>Совпадение размеров и всех пикселей.</summary>
    public bool ContentEquals(MonoBitmap? other) =>
        other is not null
        && other.Width == Width
        && other.Height == Height
        && _pixels.AsSpan().SequenceEqual(other._pixels);

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
}
