namespace Image2Gdram.Core.Packing;

/// <summary>
/// Параметры упаковки (п. 4.3 ТЗ). Значения по умолчанию — параметры SSD1306 (решение N-35).
/// Неприменимые параметры сохраняются, но упаковщик их не учитывает (решение N-36):
/// <see cref="BitsPerByte"/> — в вертикальном режиме, <see cref="PageTraversal"/> — в горизонтальном,
/// <see cref="ByteOrder"/> — для монохромного формата.
/// </summary>
public sealed record PackingOptions
{
    public static PackingOptions Default { get; } = new();

    public PixelFormat PixelFormat { get; init; } = PixelFormat.Mono1bpp;

    public PackDirection Direction { get; init; } = PackDirection.Vertical;

    public BitOrder BitOrder { get; init; } = BitOrder.LsbFirst;

    /// <summary>Число бит в байте в горизонтальном режиме: 8 или 6 (п. 4.3.3 ТЗ).</summary>
    public int BitsPerByte { get; init; } = 8;

    public PageTraversal PageTraversal { get; init; } = PageTraversal.ByPages;

    /// <summary>Инверсия итогового байта целиком, включая биты дополнения (решение F-01).</summary>
    public bool Invert { get; init; }

    public ByteOrder ByteOrder { get; init; } = ByteOrder.BigEndian;
}
