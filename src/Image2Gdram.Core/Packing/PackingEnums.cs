namespace Image2Gdram.Core.Packing;

/// <summary>Формат пикселя упакованных данных (п. 4.6 ТЗ).</summary>
public enum PixelFormat
{
    /// <summary>Монохромный, 1 бит на пиксель.</summary>
    Mono1bpp,

    /// <summary>Цветной 16-битный формат. Зарезервирован: в версии 1.0 упаковщика нет.</summary>
    Rgb565,
}

/// <summary>Порядок байтов в 16-битном слове цветных форматов (п. 4.6 ТЗ). Монохромный упаковщик его игнорирует.</summary>
public enum ByteOrder
{
    BigEndian,
    LittleEndian,
}

/// <summary>Направление сборки (п. 4.3.2 ТЗ).</summary>
public enum PackDirection
{
    /// <summary>Строчный режим: байт содержит соседние пиксели одной строки.</summary>
    Horizontal,

    /// <summary>Страничный режим: байт содержит 8 пикселей одного столбца в пределах страницы.</summary>
    Vertical,
}

/// <summary>Порядок бит в байте (п. 4.3.1 ТЗ).</summary>
public enum BitOrder
{
    /// <summary>Бит 0 — верхний или левый пиксель.</summary>
    LsbFirst,

    /// <summary>Старший используемый бит (7, при 6 битах в байте — 5) — верхний или левый пиксель.</summary>
    MsbFirst,
}

/// <summary>Порядок обхода в вертикальном режиме (п. 4.3.4 ТЗ).</summary>
public enum PageTraversal
{
    /// <summary>Все столбцы страницы 0, затем страницы 1 и т. д.</summary>
    ByPages,

    /// <summary>Для каждого столбца подряд байты всех его страниц сверху вниз.</summary>
    ByColumns,
}
