namespace Image2Gdram.Core.Packing;

/// <summary>
/// Упаковщик растра в байты GDRAM для одного формата пикселя (шаг 7 п. 4.1.2 ТЗ).
/// Новый формат добавляется новой реализацией и регистрацией в <see cref="PackerRegistry"/>.
/// </summary>
public interface IPacker
{
    PixelFormat Format { get; }

    /// <summary>Размер упакованного растра width×height в байтах.</summary>
    int GetSize(int width, int height, PackingOptions options);

    byte[] Pack(MonoBitmap bitmap, PackingOptions options);

    /// <summary>Упаковка в готовый буфер длиной ровно <see cref="GetSize"/> байт (например, символ в таблице шрифта).</summary>
    void Pack(MonoBitmap bitmap, PackingOptions options, Span<byte> destination);

    /// <summary>
    /// Обратная операция для импорта массивов (п. 4.2.2 ТЗ, источник 4): учитывает инверсию,
    /// биты дополнения игнорирует. Длина <paramref name="data"/> — ровно <see cref="GetSize"/> байт.
    /// </summary>
    MonoBitmap Unpack(ReadOnlySpan<byte> data, int width, int height, PackingOptions options);

    /// <summary>Байт и бит, в которые попадает пиксель (x, y) растра width×height.</summary>
    BitLocation Locate(int x, int y, int width, int height, PackingOptions options);
}
