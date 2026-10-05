namespace Image2Gdram.Core.Output;

/// <summary>Причина, по которой имя массива недопустимо (п. 4.4.2 ТЗ).</summary>
public enum NameValidationError
{
    None,
    Empty,

    /// <summary>Символ, отличный от латинской буквы, цифры и <c>_</c>.</summary>
    InvalidCharacter,

    StartsWithDigit,

    /// <summary>Длиннее <see cref="NameValidationResult.MaxLength"/>.</summary>
    TooLong,

    CKeyword,
    KeilC51Keyword,

    /// <summary>Зарезервированный идентификатор C: начинается с <c>__</c> или с <c>_</c> и заглавной буквы.</summary>
    ReservedCIdentifier,

    StdintTypeName,

    /// <summary>Регистр, мнемоника, директива или оператор A51 (без учёта регистра).</summary>
    A51ReservedWord,
}

public readonly record struct NameValidationResult(NameValidationError Error, int MaxLength)
{
    public bool IsValid => Error == NameValidationError.None;
}

/// <summary>
/// Проверка имени массива (п. 4.4.2 ТЗ, решения N-05, N-06). Имя проверяется по всем спискам независимо от
/// формата: одно имя используется во всех форматах вывода вкладки.
/// </summary>
public static class NameValidator
{
    public const int MaxLength = 31;

    /// <summary>Для самостоятельного модуля A51: имя сегмента <c>?CO?ИМЯ</c> на 4 символа длиннее (решение N-05).</summary>
    public const int MaxLengthA51Module = 27;

    public static int GetMaxLength(OutputFormat format) =>
        format == OutputFormat.A51Module ? MaxLengthA51Module : MaxLength;

    public static NameValidationResult Validate(string? name, OutputFormat format)
    {
        int maxLength = GetMaxLength(format);
        return new NameValidationResult(Check(name, maxLength), maxLength);
    }

    private static NameValidationError Check(string? name, int maxLength)
    {
        if (string.IsNullOrEmpty(name))
        {
            return NameValidationError.Empty;
        }

        foreach (char c in name)
        {
            if (!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_'))
            {
                return NameValidationError.InvalidCharacter;
            }
        }

        if (name[0] is >= '0' and <= '9')
        {
            return NameValidationError.StartsWithDigit;
        }

        if (name.Length > maxLength)
        {
            return NameValidationError.TooLong;
        }

        if (ReservedWords.CKeywords.Contains(name))
        {
            return NameValidationError.CKeyword;
        }

        if (ReservedWords.KeilC51Keywords.Contains(name))
        {
            return NameValidationError.KeilC51Keyword;
        }

        if (name.StartsWith("__", StringComparison.Ordinal) || (name.Length > 1 && name[0] == '_' && name[1] is >= 'A' and <= 'Z'))
        {
            return NameValidationError.ReservedCIdentifier;
        }

        if (ReservedWords.StdintTypeNames.Contains(name))
        {
            return NameValidationError.StdintTypeName;
        }

        if (ReservedWords.A51Words.Contains(name))
        {
            return NameValidationError.A51ReservedWord;
        }

        return NameValidationError.None;
    }
}
