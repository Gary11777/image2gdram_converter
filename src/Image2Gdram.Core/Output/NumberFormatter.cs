namespace Image2Gdram.Core.Output;

/// <summary>Запись чисел в выводе (п. 4.4.4 ТЗ, решение D-09).</summary>
public static class NumberFormatter
{
    private static readonly string[] CHex = Build(b => "0x" + b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
    private static readonly string[] AsmHex = Build(b => "0" + b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture) + "h");
    private static readonly string[] AsmBinary = Build(b => Convert.ToString(b, 2).PadLeft(8, '0') + "b");

    /// <summary>C: <c>0x7C</c>, заглавные цифры.</summary>
    public static string C(byte value) => CHex[value];

    /// <summary>A51, данные: всегда ведущий ноль и две цифры — <c>000h</c>, <c>07Ch</c>, <c>0FFh</c>.</summary>
    public static string AsmHexData(byte value) => AsmHex[value];

    /// <summary>A51, двоичный формат: <c>11111111b</c>.</summary>
    public static string AsmBinaryData(byte value) => AsmBinary[value];

    public static string Asm(byte value, AsmNumberFormat format) => format switch
    {
        AsmNumberFormat.Hex => AsmHex[value],
        AsmNumberFormat.Binary => AsmBinary[value],
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown assembler number format."),
    };

    /// <summary>A51, код символа в комментарии: <c>00h</c>, <c>7Fh</c>, <c>0C0h</c> — ведущий ноль только перед буквой.</summary>
    public static string AsmCode(byte code)
    {
        string hex = code.ToString("X2", System.Globalization.CultureInfo.InvariantCulture);
        return (char.IsLetter(hex[0]) ? "0" : string.Empty) + hex + "h";
    }

    private static string[] Build(Func<byte, string> format)
    {
        var table = new string[256];
        for (int i = 0; i < 256; i++)
        {
            table[i] = format((byte)i);
        }

        return table;
    }
}
