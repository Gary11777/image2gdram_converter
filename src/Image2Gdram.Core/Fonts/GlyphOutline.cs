namespace Image2Gdram.Core.Fonts;

/// <summary>Точка контура в пикселях: x вправо, y вниз.</summary>
public readonly record struct OutlinePoint(double X, double Y);

/// <summary>
/// Контур глифа — набор замкнутых многоугольников (кривые уже аппроксимированы отрезками).
/// Координаты в пикселях при 96 DPI относительно точки начала глифа: x = 0 — левый край,
/// y = 0 — базовая линия, вверх — отрицательные y. Последняя точка каждого контура неявно
/// соединяется с первой.
/// </summary>
public sealed class GlyphOutline
{
    public GlyphOutline(IEnumerable<IReadOnlyList<OutlinePoint>> contours)
    {
        ArgumentNullException.ThrowIfNull(contours);
        var copy = new List<OutlinePoint[]>();
        foreach (IReadOnlyList<OutlinePoint> contour in contours)
        {
            ArgumentNullException.ThrowIfNull(contour, nameof(contours));
            OutlinePoint[] points = contour.ToArray();
            foreach (OutlinePoint p in points)
            {
                if (!double.IsFinite(p.X) || !double.IsFinite(p.Y))
                {
                    throw new ArgumentException("Outline coordinates must be finite.", nameof(contours));
                }
            }

            copy.Add(points);
        }

        Contours = copy;
    }

    /// <summary>Пустой контур (пробел и другие символы без изображения).</summary>
    public static GlyphOutline Empty { get; } = new(Array.Empty<IReadOnlyList<OutlinePoint>>());

    public IReadOnlyList<IReadOnlyList<OutlinePoint>> Contours { get; }
}

/// <summary>Гарнитура и начертание: ключ кэша контуров. Размер — em-размер в пикселях при 96 DPI (решение D-15).</summary>
public sealed record FontFaceSpec
{
    public const int MinSizePx = 1;
    public const int MaxSizePx = 64;

    public FontFaceSpec(string family, int sizePx, bool bold = false, bool italic = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(family);
        ArgumentOutOfRangeException.ThrowIfLessThan(sizePx, MinSizePx);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sizePx, MaxSizePx);
        Family = family;
        SizePx = sizePx;
        Bold = bold;
        Italic = italic;
    }

    public string Family { get; }

    public int SizePx { get; }

    public bool Bold { get; }

    public bool Italic { get; }
}

/// <summary>Вертикальные метрики гарнитуры в пикселях: подъём над базовой линией и спуск под ней (оба ≥ 0).</summary>
public readonly record struct FontMetrics(double Ascent, double Descent);

/// <summary>
/// Доступ к контурам глифов установленных шрифтов. Реализация на WPF — <c>WpfGlyphOutlineProvider</c>
/// в сборке <c>Image2Gdram.Fonts.Wpf</c> (решения D-15, N-48); ядро от WPF не зависит.
/// Реализация обязана быть потокобезопасной.
/// </summary>
public interface IGlyphOutlineProvider
{
    /// <summary>Имена установленных гарнитур по алфавиту (для списка в интерфейсе).</summary>
    IReadOnlyList<string> GetInstalledFamilies();

    /// <summary>Метрики гарнитуры; <see langword="false"/>, если гарнитура не установлена.</summary>
    bool TryGetMetrics(FontFaceSpec face, out FontMetrics metrics);

    /// <summary>
    /// Контур символа; <see langword="null"/>, если глифа нет в самом шрифте (подстановка из других шрифтов
    /// не допускается) или гарнитура не установлена.
    /// </summary>
    GlyphOutline? GetOutline(FontFaceSpec face, char character);
}
