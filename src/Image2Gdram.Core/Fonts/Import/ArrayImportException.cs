namespace Image2Gdram.Core.Fonts.Import;

public enum ImportedArrayKind
{
    CInitializer,
    AsmDb,
}

public enum ArrayImportErrorKind
{
    /// <summary>Число вне 0…255.</summary>
    ValueOutOfRange,

    /// <summary>Лексема не является допустимым числом.</summary>
    InvalidNumber,

    /// <summary>Символ строкового литерала <c>DB</c> не входит в CP1251.</summary>
    CharacterNotInCp1251,

    /// <summary>Комментарий <c>/*</c> не закрыт.</summary>
    UnterminatedComment,

    /// <summary>Строковый литерал не закрыт.</summary>
    UnterminatedString,

    /// <summary>Лишняя или незакрытая фигурная скобка.</summary>
    UnbalancedBraces,

    /// <summary>Файл больше 16 МБ.</summary>
    FileTooLarge,

    /// <summary>Файл не удалось прочитать.</summary>
    IoError,
}

/// <summary>Ошибка одного массива: номер строки с 1 и текст лексемы. Остальные массивы файла при этом доступны.</summary>
public sealed record ArrayImportIssue(ArrayImportErrorKind Kind, int Line, string Token);

/// <summary>
/// Один массив из файла. При <see cref="Error"/> список <see cref="Values"/> пуст.
/// <see cref="Line"/> — строка имени в C или строка метки (либо первой <c>DB</c>) в ассемблере, с 1.
/// </summary>
public sealed record ImportedArray(string? Name, ImportedArrayKind Kind, int Line, IReadOnlyList<byte> Values, ArrayImportIssue? Error);

/// <summary>
/// Ошибка всего файла: незакрытый комментарий, строка или скобки, а также ошибка ввода-вывода.
/// <see cref="Line"/> с 1; 0 — строка не определена.
/// </summary>
public sealed class ArrayImportException : Exception
{
    public ArrayImportException(ArrayImportErrorKind kind, int line, string? token, Exception? inner = null)
        : base(Describe(kind, line, token), inner)
    {
        Kind = kind;
        Line = line;
        Token = token;
    }

    public ArrayImportErrorKind Kind { get; }

    public int Line { get; }

    public string? Token { get; }

    private static string Describe(ArrayImportErrorKind kind, int line, string? token)
    {
        string where = line > 0 ? $" at line {line}" : string.Empty;
        string what = string.IsNullOrEmpty(token) ? string.Empty : $" ('{token}')";
        return $"Array import failed: {kind}{where}{what}.";
    }
}
