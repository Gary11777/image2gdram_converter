namespace Image2Gdram.Core.Diagnostics;

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

/// <summary>Код предупреждения или ошибки ядра; текст для пользователя берётся из словаря интерфейса по коду.</summary>
public enum DiagnosticCode
{
    /// <summary>Массив больше 65535 байт для цели C51/A51 (решение D-12). Аргумент: размер в байтах.</summary>
    ArrayExceeds64KForC51,

    /// <summary>
    /// Символы вне CP1251 в имени файла, гарнитуры или пресета заменены на <c>?</c> (решение D-03).
    /// Аргументы: поле заголовка (<see cref="Output.HeaderField"/>), исходное значение.
    /// </summary>
    NonCp1251CharactersReplaced,

    /// <summary>Гарнитура TrueType не установлена (п. 4.2.2 ТЗ, источник 1). Аргумент: имя гарнитуры.</summary>
    FontNotFound,

    /// <summary>
    /// В шрифте нет глифов для части выбранных кодов; символы оставлены пустыми (п. 4.2.2 ТЗ, решение N-23).
    /// Аргументы: коды по возрастанию в виде <c>0xC0</c>.
    /// </summary>
    GlyphsMissingInFont,

    /// <summary>
    /// На листе не хватило ячеек для выбранных кодов (п. 4.2.2 ТЗ, источник 2).
    /// Аргументы: сколько ячеек поместилось; первый незаполненный код в виде <c>0xC0</c>.
    /// </summary>
    SheetTooSmall,

    /// <summary>
    /// В импортированном массиве не 256·N значений (п. 4.2.2 ТЗ, источник 4).
    /// Аргументы: ожидаемое и фактическое число значений.
    /// </summary>
    ImportValueCountMismatch,
}

/// <summary>Неблокирующее сообщение ядра. Аргументы — строки в <see cref="System.Globalization.CultureInfo.InvariantCulture"/>.</summary>
public sealed record Diagnostic(DiagnosticCode Code, DiagnosticSeverity Severity, IReadOnlyList<string> Arguments);
