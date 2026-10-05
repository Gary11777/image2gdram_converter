using Image2Gdram.Core.Text;

namespace Image2Gdram.Core.Output;

/// <summary>
/// Комментарий к символу шрифта: <c>/* 0xC0 'А' */</c> в C, <c>; 0C0h 'А'</c> в ассемблере;
/// для управляющих и непечатаемых кодов — только код (п. 4.4.4 ТЗ, решения D-09, D-10).
/// Символ пишется как есть, без экранирования (решение N-31).
/// </summary>
public static class GlyphCommentFormatter
{
    /// <summary>Содержимое комментария C без ограничителей: <c>0xC0 'А'</c>.</summary>
    public static string CText(byte code) => "0x" + code.ToString("X2", System.Globalization.CultureInfo.InvariantCulture) + Glyph(code);

    /// <summary>Комментарий C целиком: <c>/* 0xC0 'А' */</c>.</summary>
    public static string C(byte code) => "/* " + CText(code) + " */";

    /// <summary>Комментарий A51 целиком: <c>; 0C0h 'А'</c>.</summary>
    public static string Asm(byte code) => "; " + NumberFormatter.AsmCode(code) + Glyph(code);

    private static string Glyph(byte code) =>
        Cp1251.IsPrintable(code) && Cp1251.ToUnicode(code) is char c ? " '" + c + "'" : string.Empty;
}
