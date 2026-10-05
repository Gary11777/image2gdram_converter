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
}

/// <summary>Неблокирующее сообщение ядра. Аргументы — строки в <see cref="System.Globalization.CultureInfo.InvariantCulture"/>.</summary>
public sealed record Diagnostic(DiagnosticCode Code, DiagnosticSeverity Severity, IReadOnlyList<string> Arguments);
