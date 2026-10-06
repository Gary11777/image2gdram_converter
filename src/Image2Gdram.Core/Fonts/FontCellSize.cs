namespace Image2Gdram.Core.Fonts;

/// <summary>
/// Размер ячейки шрифта (Ш×В): только 6×8, 8×8 и 12×16 (п. 4.2.1 ТЗ). Других значений не бывает,
/// поэтому экземпляры создаются только через статические свойства и <see cref="TryGet"/>.
/// </summary>
public sealed record FontCellSize
{
    private FontCellSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public static FontCellSize Cell6x8 { get; } = new(6, 8);

    public static FontCellSize Cell8x8 { get; } = new(8, 8);

    public static FontCellSize Cell12x16 { get; } = new(12, 16);

    /// <summary>Все допустимые размеры в порядке п. 4.2.1 ТЗ.</summary>
    public static IReadOnlyList<FontCellSize> All { get; } = new[] { Cell6x8, Cell8x8, Cell12x16 };

    public int Width { get; }

    public int Height { get; }

    public static bool TryGet(int width, int height, out FontCellSize cell)
    {
        foreach (FontCellSize candidate in All)
        {
            if (candidate.Width == width && candidate.Height == height)
            {
                cell = candidate;
                return true;
            }
        }

        cell = Cell6x8;
        return false;
    }

    public static FontCellSize Get(int width, int height) =>
        TryGet(width, height, out FontCellSize cell)
            ? cell
            : throw new ArgumentOutOfRangeException(nameof(width), $"Font cell {width}x{height} is not supported (6x8, 8x8, 12x16).");

    public override string ToString() => $"{Width}x{Height}";
}
