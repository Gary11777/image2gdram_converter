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
