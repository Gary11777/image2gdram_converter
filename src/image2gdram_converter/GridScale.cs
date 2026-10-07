using Image2Gdram.Core.Packing;

namespace image2gdram_converter;

/// <summary>Целый масштаб сетки и шаг толстых линий (решения N-24 и N-54).</summary>
public static class GridScale
{
    public const int Min = 1;
    public const int Max = 32;

    /// <summary>Тонкие линии клеток видны, когда клетка не меньше этого числа единиц.</summary>
    public const int ThinFrom = 4;

    /// <summary>Толстые границы байтов видны, когда клетка не меньше этого числа единиц.</summary>
    public const int ThickFrom = 2;

    public static int Fit(double viewportWidth, double viewportHeight, int imageWidth, int imageHeight)
    {
        if (viewportWidth < 1 || viewportHeight < 1 || imageWidth < 1 || imageHeight < 1)
        {
            return Min;
        }

        int alongX = (int)Math.Floor(viewportWidth / imageWidth);
        int alongY = (int)Math.Floor(viewportHeight / imageHeight);
        int scale = Math.Min(alongX, alongY);
        if (scale < Min)
        {
            return Min;
        }

        return scale > Max ? Max : scale;
    }

    /// <summary>Пикселей между толстыми линиями: бит в байте по горизонтали, страница из 8 по вертикали.</summary>
    public static int ThickStep(PackDirection direction, int bitsPerByte) =>
        direction == PackDirection.Horizontal ? bitsPerByte : 8;
}

/// <summary>
/// Перевод между единицами WPF (1/96 дюйма) и физическими пикселями экрана при масштабе Windows 100–200 %
/// (решение N-57). Клетки растров считаются в физических пикселях, чтобы все точки были одинаковыми и чёткими.
/// </summary>
public static class DevicePixels
{
    /// <summary>Физических пикселей на клетку размером <paramref name="units"/> единиц WPF; не меньше 1.</summary>
    public static int Cell(int units, double dpiScale) =>
        Math.Max(1, (int)Math.Round(units * Normalize(dpiScale), MidpointRounding.AwayFromZero));

    public static double ToDip(double pixels, double dpiScale) => pixels / Normalize(dpiScale);

    /// <summary>Ближайшая к <paramref name="dip"/> граница физического пикселя, в единицах WPF.</summary>
    public static double Snap(double dip, double dpiScale)
    {
        double scale = Normalize(dpiScale);
        return Math.Round(dip * scale, MidpointRounding.AwayFromZero) / scale;
    }

    /// <summary>Номер клетки под точкой <paramref name="dip"/>, если клетка занимает <paramref name="cellPixels"/> физических пикселей.</summary>
    public static int CellAt(double dip, double dpiScale, int cellPixels) =>
        (int)Math.Floor(dip * Normalize(dpiScale) / Math.Max(1, cellPixels));

    private static double Normalize(double dpiScale) => dpiScale > 0 && double.IsFinite(dpiScale) ? dpiScale : 1.0;
}
