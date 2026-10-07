using Image2Gdram.Core.Packing;

namespace image2gdram_converter;

/// <summary>
/// Символ в буфере обмена (решение N-22). Свой формат — логические пиксели.
/// Текст — видимые точки: <c>#</c> точка, <c>.</c> фон, с учётом инверсии.
/// </summary>
public static class GlyphClipboard
{
    public const string Format = "Image2Gdram.Glyph";

    public static string ToPayload(MonoBitmap glyph)
    {
        ArgumentNullException.ThrowIfNull(glyph);
        var text = new System.Text.StringBuilder();
        text.Append(glyph.Width.ToString(System.Globalization.CultureInfo.InvariantCulture));
        text.Append(' ');
        text.Append(glyph.Height.ToString(System.Globalization.CultureInfo.InvariantCulture));
        for (int y = 0; y < glyph.Height; y++)
        {
            text.Append('\n');
            for (int x = 0; x < glyph.Width; x++)
            {
                text.Append(glyph[x, y] ? '1' : '0');
            }
        }

        return text.ToString();
    }

    public static string ToText(MonoBitmap glyph, bool invert)
    {
        ArgumentNullException.ThrowIfNull(glyph);
        var text = new System.Text.StringBuilder();
        for (int y = 0; y < glyph.Height; y++)
        {
            if (y > 0)
            {
                text.Append('\n');
            }

            for (int x = 0; x < glyph.Width; x++)
            {
                bool on = PixelPaint.Displayed(glyph[x, y], invert);
                text.Append(on ? '#' : '.');
            }
        }

        return text.ToString();
    }

    public static bool TryParsePayload(string? text, out MonoBitmap? glyph)
    {
        glyph = null;
        string[] lines = Split(text);
        if (lines.Length < 2)
        {
            return false;
        }

        string[] header = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (header.Length != 2
            || !int.TryParse(header[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int width)
            || !int.TryParse(header[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int height)
            || width < 1
            || height < 1
            || lines.Length != height + 1)
        {
            return false;
        }

        var bitmap = new MonoBitmap(width, height);
        for (int y = 0; y < height; y++)
        {
            string row = lines[y + 1];
            if (row.Length != width)
            {
                return false;
            }

            for (int x = 0; x < width; x++)
            {
                if (row[x] is not ('0' or '1'))
                {
                    return false;
                }

                bitmap[x, y] = row[x] == '1';
            }
        }

        glyph = bitmap;
        return true;
    }

    public static bool TryParseText(string? text, bool invert, out MonoBitmap? glyph)
    {
        glyph = null;
        string[] lines = Split(text);
        if (lines.Length == 0)
        {
            return false;
        }

        int width = lines[0].Length;
        if (width == 0)
        {
            return false;
        }

        var bitmap = new MonoBitmap(width, lines.Length);
        for (int y = 0; y < lines.Length; y++)
        {
            string row = lines[y];
            if (row.Length != width)
            {
                return false;
            }

            for (int x = 0; x < width; x++)
            {
                if (row[x] is not ('#' or '.'))
                {
                    return false;
                }

                bool on = row[x] == '#';
                bitmap[x, y] = invert ? !on : on;
            }
        }

        glyph = bitmap;
        return true;
    }

    private static string[] Split(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }
}
