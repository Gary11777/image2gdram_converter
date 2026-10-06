using System.Text;

namespace Image2Gdram.Core.Text;

/// <summary>
/// Кодовая страница Windows-1251 (п. 4.2.1, 4.4.8 ТЗ, решения D-10 и D-16). Таблица строится один раз
/// из провайдера кодовых страниц .NET; код 0x98 в CP1251 не занят и обрабатывается явно, без опоры на таблицу .NET.
/// Перекодировка текста идёт по этой же таблице, поэтому не зависит от «наилучшего соответствия» .NET.
/// </summary>
public static class Cp1251
{
    public const byte Unassigned = 0x98;

    private const char UnassignedPlaceholder = '\u0098';

    private static readonly char?[] ToUnicodeTable = BuildTable();
    private static readonly Dictionary<char, byte> FromUnicodeTable = BuildReverse(ToUnicodeTable);

    /// <summary>Регистрирует провайдер кодовых страниц .NET (повторный вызов безопасен).</summary>
    public static void RegisterEncodingProvider() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>Символ Unicode для кода; для 0x98 — <c>null</c>.</summary>
    public static char? ToUnicode(byte code) => ToUnicodeTable[code];

    public static bool TryFromUnicode(char c, out byte code) => FromUnicodeTable.TryGetValue(c, out code);

    /// <summary>
    /// Печатаемый символ для комментария с изображением символа шрифта: 0x20–0x7E и 0x80–0xFF,
    /// кроме 0x98, 0xA0 (неразрывный пробел) и 0xAD (мягкий перенос) — решение D-10.
    /// </summary>
    public static bool IsPrintable(byte code) =>
        code is >= 0x20 and <= 0x7E
        || (code >= 0x80 && code != Unassigned && code != 0xA0 && code != 0xAD);

    /// <summary>Перекодирует текст; символ вне CP1251 заменяется на <c>?</c>.</summary>
    public static byte[] GetBytes(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var bytes = new byte[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            bytes[i] = TryFromUnicode(text[i], out byte code) ? code : (byte)'?';
        }

        return bytes;
    }

    /// <summary>
    /// Декодирует байты CP1251 своей таблицей. Каждый байт даёт ровно один символ; незанятый код 0x98
    /// декодируется как U+0098, чтобы <see cref="TryFromDecoded"/> восстановил исходный байт (решение N-49).
    /// </summary>
    public static string GetString(ReadOnlySpan<byte> bytes)
    {
        return string.Create(bytes.Length, bytes.ToArray(), static (span, source) =>
        {
            for (int i = 0; i < source.Length; i++)
            {
                span[i] = ToUnicodeTable[source[i]] ?? UnassignedPlaceholder;
            }
        });
    }

    /// <summary>
    /// Код CP1251 для символа текста, полученного через <see cref="GetString"/>: как <see cref="TryFromUnicode"/>,
    /// плюс U+0098 → 0x98.
    /// </summary>
    public static bool TryFromDecoded(char c, out byte code)
    {
        if (c == UnassignedPlaceholder)
        {
            code = Unassigned;
            return true;
        }

        return TryFromUnicode(c, out code);
    }

    private static char?[] BuildTable()
    {
        RegisterEncodingProvider();
        Encoding encoding = Encoding.GetEncoding(1251, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        var table = new char?[256];
        for (int code = 0; code < 256; code++)
        {
            if (code == Unassigned)
            {
                continue;
            }

            string s = encoding.GetString(new[] { (byte)code });
            table[code] = s.Length == 1 ? s[0] : throw new InvalidOperationException($"CP1251 code 0x{code:X2} is not a single character.");
        }

        return table;
    }

    private static Dictionary<char, byte> BuildReverse(char?[] table)
    {
        var reverse = new Dictionary<char, byte>(256);
        for (int code = 0; code < 256; code++)
        {
            if (table[code] is char c)
            {
                reverse.Add(c, (byte)code);
            }
        }

        return reverse;
    }
}
