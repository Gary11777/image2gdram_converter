namespace Image2Gdram.Core.Output;

/// <summary>Параметры вывода (п. 4.4 ТЗ). Значения по умолчанию — решение N-16.</summary>
public sealed record OutputOptions
{
    public const int MinBytesPerLine = 1;
    public const int MaxBytesPerLine = 16;

    public static OutputOptions Default { get; } = new();

    public OutputFormat Format { get; init; } = OutputFormat.CKeilC51;

    /// <summary>Имя массива; проверяется <see cref="NameValidator"/>.</summary>
    public string ArrayName { get; init; } = "image";

    public OutputEncoding Encoding { get; init; } = OutputEncoding.Cp1251;

    /// <summary>Байт в строке для изображений, 1…16 (п. 4.4.4 ТЗ). Для шрифтов не применяется (решение D-08).</summary>
    public int BytesPerLine { get; init; } = 16;

    public AsmNumberFormat AsmNumberFormat { get; init; } = AsmNumberFormat.Hex;

    public AsmFileExtension AsmFileExtension { get; init; } = AsmFileExtension.A51;

    public Stm32ElementType Stm32ElementType { get; init; } = Stm32ElementType.Uint8T;

    /// <summary>Строка «Дата: …» в заголовке (решение N-04).</summary>
    public bool IncludeDate { get; init; } = true;

    /// <summary>Время генерации для строки даты; <c>null</c> — текущее местное время.</summary>
    public DateTime? GeneratedAt { get; init; }
}
