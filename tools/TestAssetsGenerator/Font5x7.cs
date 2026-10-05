namespace Image2Gdram.TestAssets;

/// <summary>Встроенный растровый шрифт 5×7 для надписей тестовых изображений. Не зависит от системных шрифтов.</summary>
public static class Font5x7
{
    public const int Width = 5;
    public const int Height = 7;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['0'] = Rows(".XXX.", "X...X", "X..XX", "X.X.X", "XX..X", "X...X", ".XXX."),
        ['1'] = Rows("..X..", ".XX..", "..X..", "..X..", "..X..", "..X..", ".XXX."),
        ['2'] = Rows(".XXX.", "X...X", "....X", "...X.", "..X..", ".X...", "XXXXX"),
        ['3'] = Rows(".XXX.", "X...X", "....X", "..XX.", "....X", "X...X", ".XXX."),
        ['4'] = Rows("...X.", "..XX.", ".X.X.", "X..X.", "XXXXX", "...X.", "...X."),
        ['5'] = Rows("XXXXX", "X....", "XXXX.", "....X", "....X", "X...X", ".XXX."),
        ['6'] = Rows(".XXX.", "X....", "X....", "XXXX.", "X...X", "X...X", ".XXX."),
        ['7'] = Rows("XXXXX", "....X", "...X.", "..X..", ".X...", ".X...", ".X..."),
        ['8'] = Rows(".XXX.", "X...X", "X...X", ".XXX.", "X...X", "X...X", ".XXX."),
        ['9'] = Rows(".XXX.", "X...X", "X...X", ".XXXX", "....X", "....X", ".XXX."),
        ['x'] = Rows("X...X", ".X.X.", "..X..", ".X.X.", "X...X", ".....", "....."),
    };

    public static int Measure(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        return text.Length * (Width + 1) - 1;
    }

    public static bool IsInk(char glyph, int x, int y)
    {
        if (!Glyphs.TryGetValue(glyph, out string[]? rows))
        {
            throw new ArgumentException($"Font 5x7 has no glyph '{glyph}'.", nameof(glyph));
        }

        if ((uint)x >= Width || (uint)y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        return rows[y][x] == 'X';
    }

    private static string[] Rows(params string[] rows)
    {
        if (rows.Length != Height || rows.Any(row => row.Length != Width))
        {
            throw new ArgumentException("Glyph must be 5 by 7.");
        }

        return rows;
    }
}
