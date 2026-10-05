namespace Image2Gdram.Core.Output;

/// <summary>Разбиение данных на строки (п. 4.4.4 ТЗ).</summary>
public static class DataLayout
{
    public const int MaxBytesPerFontLine = 16;

    /// <summary>
    /// Длины строк одного символа шрифта (решение D-08): <c>n = ⌈N / 16⌉</c> строк по <c>N / n</c> байт,
    /// при неделимости первые строки на 1 байт длиннее. 24 → 12 + 12, 32 → 16 + 16, 6 → 6.
    /// </summary>
    public static int[] SplitGlyph(int bytesPerChar)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bytesPerChar, 1);
        int lines = (bytesPerChar + MaxBytesPerFontLine - 1) / MaxBytesPerFontLine;
        int baseLength = bytesPerChar / lines;
        int longer = bytesPerChar % lines;
        var lengths = new int[lines];
        for (int i = 0; i < lines; i++)
        {
            lengths[i] = baseLength + (i < longer ? 1 : 0);
        }

        return lengths;
    }

    /// <summary>Длины строк изображения: по <paramref name="bytesPerLine"/>, последняя — остаток.</summary>
    public static int[] SplitImage(int byteCount, int bytesPerLine)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(byteCount, 1);
        ValidateBytesPerLine(bytesPerLine);
        int lines = (byteCount + bytesPerLine - 1) / bytesPerLine;
        var lengths = new int[lines];
        for (int i = 0; i < lines; i++)
        {
            lengths[i] = Math.Min(bytesPerLine, byteCount - i * bytesPerLine);
        }

        return lengths;
    }

    internal static void ValidateBytesPerLine(int bytesPerLine)
    {
        if (bytesPerLine is < OutputOptions.MinBytesPerLine or > OutputOptions.MaxBytesPerLine)
        {
            throw new ArgumentOutOfRangeException(nameof(bytesPerLine), bytesPerLine, "Bytes per line must be 1…16.");
        }
    }
}
