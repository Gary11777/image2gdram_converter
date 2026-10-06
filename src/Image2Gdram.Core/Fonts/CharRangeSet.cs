using System.Globalization;
using Image2Gdram.Core.Text;

namespace Image2Gdram.Core.Fonts;

/// <summary>Предустановленные диапазоны п. 4.2.3 ТЗ; выбираются в любом сочетании (решение N-13).</summary>
[Flags]
public enum CharRangePreset
{
    None = 0,

    /// <summary>0x20–0x7E — латиница, цифры, знаки препинания.</summary>
    Latin = 1,

    /// <summary>0xC0–0xFF, 0xA8 (Ё), 0xB8 (ё) — кириллица.</summary>
    Cyrillic = 2,

    /// <summary>0x80–0xBF, кроме 0x98, — прочие символы CP1251.</summary>
    OtherCp1251 = 4,
}

public enum CharRangeParseErrorKind
{
    /// <summary>Не число в формате <c>0x..</c>, <c>..h</c> или десятичном.</summary>
    InvalidNumber,

    /// <summary>Код вне 0…255.</summary>
    OutOfRange,

    /// <summary>Начало диапазона больше конца.</summary>
    ReversedRange,

    /// <summary>Лишний или недостающий знак <c>-</c> (например, <c>0x41-</c> или <c>-0x41</c>).</summary>
    MisplacedDash,
}

/// <summary>Ошибка разбора произвольного диапазона: вид, позиция (с 0) и текст ошибочного фрагмента.</summary>
public sealed record CharRangeParseError(CharRangeParseErrorKind Kind, int Position, string Token);

/// <summary>
/// Набор кодов, заполняемых из источников 1 и 2 п. 4.2.2 ТЗ: предустановленные диапазоны плюс
/// произвольный (решения N-13, N-50). Код 0x98 в предустановленные не входит никогда; в произвольный — если указан явно.
/// </summary>
public sealed class CharRangeSet
{
    private readonly bool[] _contains = new bool[FontTable.CharCount];

    public CharRangeSet(CharRangePreset presets, IEnumerable<int>? customCodes = null)
    {
        if ((presets & ~(CharRangePreset.Latin | CharRangePreset.Cyrillic | CharRangePreset.OtherCp1251)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(presets), presets, "Unknown character range preset.");
        }

        Presets = presets;
        var custom = new SortedSet<int>();
        foreach (int code in customCodes ?? Array.Empty<int>())
        {
            custom.Add(FontTable.CheckCode(code));
        }

        CustomCodes = custom.ToArray();
        for (int code = 0; code < FontTable.CharCount; code++)
        {
            _contains[code] = InPresets(presets, code) || custom.Contains(code);
        }

        Codes = Enumerable.Range(0, FontTable.CharCount).Where(c => _contains[c]).ToArray();
    }

    /// <summary>По умолчанию — латиница и кириллица (решение N-13).</summary>
    public static CharRangeSet Default { get; } = new(CharRangePreset.Latin | CharRangePreset.Cyrillic);

    public CharRangePreset Presets { get; }

    /// <summary>Коды произвольного диапазона по возрастанию, без повторов.</summary>
    public IReadOnlyList<int> CustomCodes { get; }

    /// <summary>Все выбранные коды по возрастанию, без повторов.</summary>
    public IReadOnlyList<int> Codes { get; }

    public bool Contains(int code) => _contains[FontTable.CheckCode(code)];

    public static IReadOnlyList<int> GetPresetCodes(CharRangePreset preset) =>
        Enumerable.Range(0, FontTable.CharCount).Where(c => InPresets(preset, c)).ToArray();

    /// <summary>
    /// Разбирает произвольный диапазон вида <c>0x41-0x5A, 0xA8, 200</c>: коды <c>0x..</c>, <c>..h</c>
    /// (начинается с цифры) или десятичные; диапазон через <c>-</c> (пробелы вокруг допустимы);
    /// элементы разделяются запятыми и/или пробельными символами. Пустая строка — пустой набор.
    /// </summary>
    public static bool TryParseCustom(string text, out IReadOnlyList<int> codes, out CharRangeParseError? error)
    {
        ArgumentNullException.ThrowIfNull(text);
        var result = new SortedSet<int>();
        codes = Array.Empty<int>();
        error = null;

        List<Token> tokens = Tokenize(text);
        int i = 0;
        while (i < tokens.Count)
        {
            Token first = tokens[i];
            if (first.IsDash)
            {
                error = new CharRangeParseError(CharRangeParseErrorKind.MisplacedDash, first.Position, first.Text);
                return false;
            }

            if (!TryParseCode(first, out int start, out error))
            {
                return false;
            }

            int end = start;
            if (i + 1 < tokens.Count && tokens[i + 1].IsDash)
            {
                Token dash = tokens[i + 1];
                if (dash.SeparatedBefore || i + 2 >= tokens.Count || tokens[i + 2].IsDash || tokens[i + 2].SeparatedBefore)
                {
                    error = new CharRangeParseError(CharRangeParseErrorKind.MisplacedDash, dash.Position, dash.Text);
                    return false;
                }

                Token last = tokens[i + 2];
                if (!TryParseCode(last, out end, out error))
                {
                    return false;
                }

                if (end < start)
                {
                    error = new CharRangeParseError(
                        CharRangeParseErrorKind.ReversedRange,
                        first.Position,
                        text[first.Position..(last.Position + last.Text.Length)]);
                    return false;
                }

                i += 3;
            }
            else
            {
                i++;
            }

            for (int code = start; code <= end; code++)
            {
                result.Add(code);
            }
        }

        codes = result.ToArray();
        return true;
    }

    private static bool InPresets(CharRangePreset presets, int code) =>
        (presets.HasFlag(CharRangePreset.Latin) && code is >= 0x20 and <= 0x7E)
        || (presets.HasFlag(CharRangePreset.Cyrillic) && (code is >= 0xC0 and <= 0xFF || code == 0xA8 || code == 0xB8))
        || (presets.HasFlag(CharRangePreset.OtherCp1251) && code is >= 0x80 and <= 0xBF && code != Cp1251.Unassigned);

    private static bool TryParseCode(Token token, out int code, out CharRangeParseError? error)
    {
        error = null;
        string s = token.Text;
        ReadOnlySpan<char> digits;
        NumberStyles style;
        if (s.Length > 2 && s[0] == '0' && (s[1] == 'x' || s[1] == 'X'))
        {
            digits = s.AsSpan(2);
            style = NumberStyles.AllowHexSpecifier;
        }
        else if (s.Length > 1 && (s[^1] == 'h' || s[^1] == 'H') && char.IsAsciiDigit(s[0]))
        {
            digits = s.AsSpan(0, s.Length - 1);
            style = NumberStyles.AllowHexSpecifier;
        }
        else
        {
            digits = s;
            style = NumberStyles.None;
        }

        bool valid = !digits.IsEmpty && (style == NumberStyles.None ? IsDecimal(digits) : IsHex(digits));
        if (!valid)
        {
            code = 0;
            error = new CharRangeParseError(CharRangeParseErrorKind.InvalidNumber, token.Position, s);
            return false;
        }

        digits = digits.TrimStart('0');
        long value = 0;
        if (digits.Length > 8 || (!digits.IsEmpty && !long.TryParse(digits, style, CultureInfo.InvariantCulture, out value)) || value > 0xFF)
        {
            code = 0;
            error = new CharRangeParseError(CharRangeParseErrorKind.OutOfRange, token.Position, s);
            return false;
        }

        code = (int)value;
        return true;
    }

    private static bool IsHex(ReadOnlySpan<char> s)
    {
        foreach (char c in s)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDecimal(ReadOnlySpan<char> s)
    {
        foreach (char c in s)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        bool separated = false;
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (c == ',')
            {
                separated = true;
                i++;
            }
            else if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (c == '-')
            {
                tokens.Add(new Token("-", i, IsDash: true, separated));
                separated = false;
                i++;
            }
            else
            {
                int start = i;
                while (i < text.Length && text[i] != ',' && text[i] != '-' && !char.IsWhiteSpace(text[i]))
                {
                    i++;
                }

                tokens.Add(new Token(text[start..i], start, IsDash: false, separated));
                separated = false;
            }
        }

        return tokens;
    }

    private readonly record struct Token(string Text, int Position, bool IsDash, bool SeparatedBefore);
}
