namespace image2gdram_converter;

/// <summary>Как штрих мыши ложится на логический пиксель (решения N-08 и N-54).</summary>
public enum StrokePaint
{
    /// <summary>Щелчок левой кнопкой: переключить видимую точку.</summary>
    Toggle,

    /// <summary>Протягивание левой кнопкой: видимая точка.</summary>
    Dot,

    /// <summary>Правая кнопка: видимый фон.</summary>
    Background,
}

/// <summary>
/// Связь логического «активного» пикселя и видимой точки после инверсии.
/// Бит 1 в упакованном байте — точка, бит 0 — фон.
/// </summary>
public static class PixelPaint
{
    public static bool Displayed(bool logicalActive, bool invert) => invert ? !logicalActive : logicalActive;

    /// <summary>Какое логическое значение записать, чтобы в сетке получилась точка или фон.</summary>
    public static bool Logical(bool invert, StrokePaint paint) => paint switch
    {
        StrokePaint.Dot => !invert,
        StrokePaint.Background => invert,
        _ => throw new ArgumentOutOfRangeException(nameof(paint), paint, "Toggle depends on the current pixel."),
    };
}
