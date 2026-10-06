using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Fonts;

public enum ShiftDirection
{
    Left,
    Right,
    Up,
    Down,
}

/// <summary>
/// Операции редактора символа (п. 5.4 ТЗ). Каждая возвращает новый растр и не меняет исходный.
/// При сдвиге ушедшие за край пиксели теряются, освободившийся край заполняется фоном (решение N-46).
/// </summary>
public static class GlyphOps
{
    public static MonoBitmap Clear(MonoBitmap glyph)
    {
        ArgumentNullException.ThrowIfNull(glyph);
        return new MonoBitmap(glyph.Width, glyph.Height);
    }

    public static MonoBitmap Invert(MonoBitmap glyph)
    {
        ArgumentNullException.ThrowIfNull(glyph);
        var result = new MonoBitmap(glyph.Width, glyph.Height);
        for (int y = 0; y < glyph.Height; y++)
        {
            for (int x = 0; x < glyph.Width; x++)
            {
                result[x, y] = !glyph[x, y];
            }
        }

        return result;
    }

    public static MonoBitmap Toggle(MonoBitmap glyph, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(glyph);
        MonoBitmap result = glyph.Clone();
        result[x, y] = !result[x, y];
        return result;
    }

    public static MonoBitmap Shift(MonoBitmap glyph, ShiftDirection direction)
    {
        ArgumentNullException.ThrowIfNull(glyph);
        (int dx, int dy) = direction switch
        {
            ShiftDirection.Left => (-1, 0),
            ShiftDirection.Right => (1, 0),
            ShiftDirection.Up => (0, -1),
            ShiftDirection.Down => (0, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };

        var result = new MonoBitmap(glyph.Width, glyph.Height);
        for (int y = 0; y < glyph.Height; y++)
        {
            int sy = y - dy;
            if ((uint)sy >= (uint)glyph.Height)
            {
                continue;
            }

            for (int x = 0; x < glyph.Width; x++)
            {
                int sx = x - dx;
                if ((uint)sx < (uint)glyph.Width)
                {
                    result[x, y] = glyph[sx, sy];
                }
            }
        }

        return result;
    }

    /// <summary>Вставка растра другого размера: в левый верхний угол ячейки с обрезкой, остальное — фон (решение N-22).</summary>
    public static MonoBitmap Fit(MonoBitmap glyph, FontCellSize cell)
    {
        ArgumentNullException.ThrowIfNull(glyph);
        ArgumentNullException.ThrowIfNull(cell);
        var result = new MonoBitmap(cell.Width, cell.Height);
        int width = Math.Min(glyph.Width, cell.Width);
        int height = Math.Min(glyph.Height, cell.Height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = glyph[x, y];
            }
        }

        return result;
    }
}
