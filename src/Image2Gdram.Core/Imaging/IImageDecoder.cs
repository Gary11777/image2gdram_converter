namespace Image2Gdram.Core.Imaging;

/// <summary>
/// Загрузка BMP, PNG, JPEG и GIF. Реализация на WIC живёт в проекте net8.0-windows (решение N-39).
/// </summary>
public interface IImageDecoder
{
    /// <summary>Размер и число кадров. Пиксели не копируются. Сторона больше 8192 — <see cref="ImageLoadError.TooLarge"/>.</summary>
    ImageInfo Identify(string path);

    /// <summary>Все кадры в прямом RGBA. Исходный файл только читается и после возврата не занят.</summary>
    DecodedImage Decode(string path, CancellationToken cancellationToken = default);
}
