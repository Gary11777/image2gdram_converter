using System.Globalization;

namespace Image2Gdram.Core.Text;

/// <summary>Русские формы множественного числа для заголовка-комментария (решение D-04).</summary>
public static class RussianPlural
{
    /// <summary>
    /// Форма слова для числа <paramref name="count"/>: 1, 21, 31… — <paramref name="one"/>;
    /// 2–4, 22–24… — <paramref name="few"/>; 0, 5–20, 25–30… — <paramref name="many"/> (11–14 — всегда <paramref name="many"/>).
    /// </summary>
    public static string Select(long count, string one, string few, string many)
    {
        long n = Math.Abs(count);
        long lastTwo = n % 100;
        if (lastTwo is >= 11 and <= 14)
        {
            return many;
        }

        return (n % 10) switch
        {
            1 => one,
            2 or 3 or 4 => few,
            _ => many,
        };
    }

    /// <summary>Число и слово: «1024 байта», «1536 байт».</summary>
    public static string Format(long count, string one, string few, string many) =>
        count.ToString(CultureInfo.InvariantCulture) + " " + Select(count, one, few, many);

    public static string Bytes(long count) => Format(count, "байт", "байта", "байт");

    public static string Symbols(long count) => Format(count, "символ", "символа", "символов");

    public static string Frames(long count) => Format(count, "кадр", "кадра", "кадров");
}
