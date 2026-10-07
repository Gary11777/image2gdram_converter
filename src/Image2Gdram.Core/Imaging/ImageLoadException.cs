namespace Image2Gdram.Core.Imaging;

/// <summary>Причина, по которой файл изображения не загружен (п. 4.1.1, раздел 6 ТЗ).</summary>
public enum ImageLoadError
{
    /// <summary>Сторона больше <see cref="ImageLimits.MaxSide"/>.</summary>
    TooLarge,

    /// <summary>Суммарный объём кадров больше <see cref="ImageLimits.MaxRgbaBytes"/>.</summary>
    MemoryLimit,

    /// <summary>Не BMP, PNG, JPEG и не GIF.</summary>
    Unsupported,

    /// <summary>Формат узнан, но содержимое оборвано или неверно.</summary>
    Corrupted,

    /// <summary>Файл не удалось прочитать.</summary>
    IoError,
}

/// <summary>
/// Ошибка загрузки изображения. Пользователь видит фразу словаря по <see cref="Error"/>; текст исключения — для разработчика,
/// у <see cref="ImageLoadError.IoError"/> причину берут из внутреннего исключения.
/// </summary>
public sealed class ImageLoadException : Exception
{
    public ImageLoadException(ImageLoadError error, string message, string? path = null, Exception? inner = null)
        : base(message, inner)
    {
        Error = error;
        Path = path;
    }

    public ImageLoadError Error { get; }

    public string? Path { get; }

    public int? Width { get; init; }

    public int? Height { get; init; }
}
