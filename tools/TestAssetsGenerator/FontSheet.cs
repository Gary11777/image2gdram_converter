namespace Image2Gdram.TestAssets;

/// <summary>
/// Растровый лист шрифта: 16 символов в строке, без отступов и интервалов (решение N-60).
/// Глиф — встроенный растр 5x7, не системный шрифт.
/// </summary>
public sealed class FontSheet
{
    public const int Columns = 16;
    public const int Rows = 16;

    private FontSheet(int cellWidth, int cellHeight, string fileName, string referenceId)
    {
        CellWidth = cellWidth;
        CellHeight = cellHeight;
        FileName = fileName;
        ReferenceId = referenceId;
    }

    public static IReadOnlyList<FontSheet> All { get; } =
    [
        new FontSheet(6, 8, "font_sheet_6x8.png", "font_6x8"),
        new FontSheet(8, 8, "font_sheet_8x8.png", "font_8x8"),
        new FontSheet(12, 16, "font_sheet_12x16.png", "font_12x16"),
    ];

    public int CellWidth { get; }

    public int CellHeight { get; }

    public string FileName { get; }

    /// <summary>Имя папки эталона, например <c>font_6x8</c>.</summary>
    public string ReferenceId { get; }

    public int Width => Columns * CellWidth;

    public int Height => Rows * CellHeight;

    public bool IsActive(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"Point ({x}, {y}) is outside {Width}x{Height}.");
        }

        int code = (y / CellHeight) * Columns + x / CellWidth;
        return IsCellInk(code, x % CellWidth, y % CellHeight);
    }

    /// <summary>Пиксель ячейки символа <paramref name="code"/>: x слева направо, y сверху вниз.</summary>
    public bool IsCellInk(int code, int x, int y)
    {
        if ((uint)code > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(code));
        }

        if ((uint)x >= (uint)CellWidth || (uint)y >= (uint)CellHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (!TrySourcePixel(x, y, out int sx, out int sy))
        {
            return false;
        }

        return Glyph5x7.IsInk(code, sx, sy);
    }

    /// <summary>
    /// 6x8 — глиф 5x7 в левом верхнем углу.
    /// 8x8 — тот же глиф со столбцом фона слева.
    /// 12x16 — каждый пиксель глифа становится блоком 2x2, блок начинается в (1, 1).
    /// </summary>
    private bool TrySourcePixel(int x, int y, out int sx, out int sy)
    {
        if (CellWidth == 6 && CellHeight == 8)
        {
            sx = x;
            sy = y;
            return sx < Glyph5x7.Width && sy < Glyph5x7.Height;
        }

        if (CellWidth == 8 && CellHeight == 8)
        {
            sx = x - 1;
            sy = y;
            return sx >= 0 && sx < Glyph5x7.Width && sy < Glyph5x7.Height;
        }

        if (CellWidth == 12 && CellHeight == 16)
        {
            int ox = x - 1;
            int oy = y - 1;
            sx = ox / 2;
            sy = oy / 2;
            return ox >= 0 && oy >= 0
                && ox < Glyph5x7.Width * 2
                && oy < Glyph5x7.Height * 2;
        }

        throw new InvalidOperationException($"Unsupported cell {CellWidth}x{CellHeight}.");
    }
}
