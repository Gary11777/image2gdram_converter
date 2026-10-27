namespace Image2Gdram.Core.Processing;

/// <summary>Цвет фона шага 1 (п. 4.1.1 ТЗ). Белый — по умолчанию.</summary>
public enum BackgroundColor
{
    White,
    Black,
}

/// <summary>Поворот по часовой стрелке (решение N-10).</summary>
public enum Rotation
{
    Rotate0 = 0,
    Rotate90 = 90,
    Rotate180 = 180,
    Rotate270 = 270,
}

/// <summary>Как получить целевой размер в файлах версий 1 и 2. В конвейере 1.0 заменено прямоугольником (N-80, K-07).</summary>
public enum TargetSizeMode
{
    /// <summary>Ширина и высота заданы явно (пресет или ввод).</summary>
    Manual,

    /// <summary>Размер совпадает с изображением после поворота. Сторона больше 1024 — ошибка (решение D-07).</summary>
    MatchSource,
}

/// <summary>Режим вписывания в файлах версий 1 и 2. В интерфейсе заменён прямоугольником (N-80, K-07).</summary>
public enum FitMode
{
    /// <summary>Без масштабирования: размещение и обрезка.</summary>
    None,

    /// <summary>Вписать с сохранением пропорций.</summary>
    Fit,

    /// <summary>Растянуть без сохранения пропорций.</summary>
    Stretch,

    /// <summary>Заполнить с сохранением пропорций и обрезкой лишнего.</summary>
    Fill,
}

/// <summary>Девять положений в файлах версий 1 и 2. В интерфейсе картинки заменено перемещением прямоугольника (N-80, K-07).</summary>
public enum Alignment
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
}

/// <summary>Алгоритм масштабирования (п. 4.1.3 ТЗ). По умолчанию — усреднение по площади.</summary>
public enum ResampleMode
{
    AreaAverage,
    NearestNeighbor,
}

/// <summary>Режим бинаризации (п. 4.1.4 ТЗ).</summary>
public enum BinarizeMode
{
    Threshold,
    FloydSteinberg,
    Atkinson,
    Bayer4,
    Bayer8,
}
