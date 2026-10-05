using Image2Gdram.Core.Imaging;

namespace Image2Gdram.Core.Processing;

/// <summary>
/// Шаг 3: масштаб, выравнивание и смещение (п. 4.1.3 ТЗ, решение F-08).
/// Непокрытые пиксели получают цвет фона шага 1 (решение D-06).
/// </summary>
public static class ResizeStep
{
    public readonly record struct ScaledSize(int Width, int Height);

    public static RgbaImage Apply(
        RgbaImage source,
        int targetWidth,
        int targetHeight,
        FitMode fit,
        Alignment alignment,
        int offsetX,
        int offsetY,
        ResampleMode resample,
        BackgroundColor background,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(targetWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(targetHeight, 1);

        ScaledSize scaled = GetScaledSize(source.Width, source.Height, targetWidth, targetHeight, fit);
        RgbaImage content = scaled.Width == source.Width && scaled.Height == source.Height
            ? source
            : Resample(source, scaled.Width, scaled.Height, resample, cancellationToken);

        byte bg = BackgroundCompositor.BackgroundValue(background);
        var canvas = new RgbaImage(targetWidth, targetHeight, bg, bg, bg, 255);
        (int originX, int originY) = GetOrigin(alignment, targetWidth, targetHeight, content.Width, content.Height);
        Blit(content, canvas, originX + offsetX, originY + offsetY, cancellationToken);
        return canvas;
    }

    /// <summary>Размер изображения после масштаба и до обрезки по холсту.</summary>
    public static ScaledSize GetScaledSize(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight, FitMode fit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sourceWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(sourceHeight, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(targetWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(targetHeight, 1);

        return fit switch
        {
            FitMode.None => new ScaledSize(sourceWidth, sourceHeight),
            FitMode.Stretch => new ScaledSize(targetWidth, targetHeight),
            FitMode.Fit => ScaleTo(sourceWidth, sourceHeight, targetWidth, targetHeight, useMax: false),
            FitMode.Fill => ScaleTo(sourceWidth, sourceHeight, targetWidth, targetHeight, useMax: true),
            _ => throw new ArgumentOutOfRangeException(nameof(fit), fit, "Unknown fit mode."),
        };
    }

    /// <summary>
    /// Левый верхний угол содержимого на холсте до смещения.
    /// При центрировании нечётный остаток остаётся справа и снизу (влево и вверх).
    /// </summary>
    public static (int X, int Y) GetOrigin(Alignment alignment, int canvasWidth, int canvasHeight, int contentWidth, int contentHeight)
    {
        (HorizontalAlign horizontal, VerticalAlign vertical) = alignment switch
        {
            Alignment.TopLeft => (HorizontalAlign.Left, VerticalAlign.Top),
            Alignment.TopCenter => (HorizontalAlign.Center, VerticalAlign.Top),
            Alignment.TopRight => (HorizontalAlign.Right, VerticalAlign.Top),
            Alignment.MiddleLeft => (HorizontalAlign.Left, VerticalAlign.Middle),
            Alignment.Center => (HorizontalAlign.Center, VerticalAlign.Middle),
            Alignment.MiddleRight => (HorizontalAlign.Right, VerticalAlign.Middle),
            Alignment.BottomLeft => (HorizontalAlign.Left, VerticalAlign.Bottom),
            Alignment.BottomCenter => (HorizontalAlign.Center, VerticalAlign.Bottom),
            Alignment.BottomRight => (HorizontalAlign.Right, VerticalAlign.Bottom),
            _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, "Unknown alignment."),
        };

        int x = horizontal switch
        {
            HorizontalAlign.Left => 0,
            HorizontalAlign.Center => FloorDiv2(canvasWidth - contentWidth),
            HorizontalAlign.Right => canvasWidth - contentWidth,
            _ => throw new ArgumentOutOfRangeException(nameof(alignment)),
        };
        int y = vertical switch
        {
            VerticalAlign.Top => 0,
            VerticalAlign.Middle => FloorDiv2(canvasHeight - contentHeight),
            VerticalAlign.Bottom => canvasHeight - contentHeight,
            _ => throw new ArgumentOutOfRangeException(nameof(alignment)),
        };
        return (x, y);
    }

    public static RgbaImage Resample(RgbaImage source, int width, int height, ResampleMode mode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        return mode switch
        {
            ResampleMode.NearestNeighbor => Nearest(source, width, height, cancellationToken),
            ResampleMode.AreaAverage => AreaAverage(source, width, height, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown resample mode."),
        };
    }

    private static ScaledSize ScaleTo(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight, bool useMax)
    {
        long crossWidth = (long)targetWidth * sourceHeight;
        long crossHeight = (long)targetHeight * sourceWidth;
        bool widthDefines = useMax ? crossWidth >= crossHeight : crossWidth <= crossHeight;
        if (widthDefines)
        {
            int height = Math.Max(1, RoundHalfUp((long)sourceHeight * targetWidth, sourceWidth));
            return new ScaledSize(targetWidth, height);
        }

        int width = Math.Max(1, RoundHalfUp((long)sourceWidth * targetHeight, sourceHeight));
        return new ScaledSize(width, targetHeight);
    }

    /// <summary>Округление <c>numer / denom</c> к ближайшему, половина — вверх. Оба аргумента положительны.</summary>
    public static int RoundHalfUp(long numer, long denom)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(denom, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(numer);
        return (int)((numer + denom / 2) / denom);
    }

    private static RgbaImage Nearest(RgbaImage source, int width, int height, CancellationToken cancellationToken)
    {
        var destination = new RgbaImage(width, height);
        byte[] dst = destination.PixelBuffer;
        int srcW = source.Width;
        int srcH = source.Height;
        for (int y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int sy = Sample(y, srcH, height);
            for (int x = 0; x < width; x++)
            {
                int sx = Sample(x, srcW, width);
                source.GetPixel(sx, sy, out byte r, out byte g, out byte b, out byte a);
                int i = (y * width + x) * RgbaImage.BytesPerPixel;
                dst[i] = r;
                dst[i + 1] = g;
                dst[i + 2] = b;
                dst[i + 3] = a;
            }
        }

        return destination;
    }

    /// <summary>Индекс исходного пикселя по центру целевого: <c>((2·t + 1) · src) / (2 · dst)</c>.</summary>
    public static int Sample(int targetIndex, int sourceSize, int targetSize) =>
        (int)(((2L * targetIndex + 1) * sourceSize) / (2L * targetSize));

    private static RgbaImage AreaAverage(RgbaImage source, int width, int height, CancellationToken cancellationToken)
    {
        var destination = new RgbaImage(width, height);
        byte[] dst = destination.PixelBuffer;
        int srcW = source.Width;
        int srcH = source.Height;
        ReadOnlySpan<byte> src = source.Pixels;
        for (int y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double y0 = y * (double)srcH / height;
            double y1 = (y + 1) * (double)srcH / height;
            int sy0 = (int)Math.Floor(y0);
            int sy1 = (int)Math.Ceiling(y1) - 1;
            if (sy0 < 0)
            {
                sy0 = 0;
            }

            if (sy1 >= srcH)
            {
                sy1 = srcH - 1;
            }

            for (int x = 0; x < width; x++)
            {
                double x0 = x * (double)srcW / width;
                double x1 = (x + 1) * (double)srcW / width;
                int sx0 = (int)Math.Floor(x0);
                int sx1 = (int)Math.Ceiling(x1) - 1;
                if (sx0 < 0)
                {
                    sx0 = 0;
                }

                if (sx1 >= srcW)
                {
                    sx1 = srcW - 1;
                }

                double sumR = 0;
                double sumG = 0;
                double sumB = 0;
                double sumA = 0;
                double sumW = 0;
                for (int sy = sy0; sy <= sy1; sy++)
                {
                    double wy = Overlap(sy, sy + 1, y0, y1);
                    if (wy <= 0)
                    {
                        continue;
                    }

                    int row = sy * srcW * RgbaImage.BytesPerPixel;
                    for (int sx = sx0; sx <= sx1; sx++)
                    {
                        double wx = Overlap(sx, sx + 1, x0, x1);
                        double weight = wx * wy;
                        if (weight <= 0)
                        {
                            continue;
                        }

                        int i = row + sx * RgbaImage.BytesPerPixel;
                        sumR += src[i] * weight;
                        sumG += src[i + 1] * weight;
                        sumB += src[i + 2] * weight;
                        sumA += src[i + 3] * weight;
                        sumW += weight;
                    }
                }

                int o = (y * width + x) * RgbaImage.BytesPerPixel;
                dst[o] = RoundChannel(sumR, sumW);
                dst[o + 1] = RoundChannel(sumG, sumW);
                dst[o + 2] = RoundChannel(sumB, sumW);
                dst[o + 3] = RoundChannel(sumA, sumW);
            }
        }

        return destination;
    }

    private static double Overlap(double start, double end, double windowStart, double windowEnd)
    {
        double lo = start > windowStart ? start : windowStart;
        double hi = end < windowEnd ? end : windowEnd;
        return hi > lo ? hi - lo : 0;
    }

    private static byte RoundChannel(double sum, double weight)
    {
        if (weight <= 0)
        {
            return 0;
        }

        double value = sum / weight;
        if (value <= 0)
        {
            return 0;
        }

        if (value >= 255)
        {
            return 255;
        }

        return (byte)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private static void Blit(RgbaImage content, RgbaImage canvas, int destX, int destY, CancellationToken cancellationToken)
    {
        byte[] dst = canvas.PixelBuffer;
        ReadOnlySpan<byte> src = content.Pixels;
        for (int y = 0; y < content.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int cy = destY + y;
            if ((uint)cy >= (uint)canvas.Height)
            {
                continue;
            }

            for (int x = 0; x < content.Width; x++)
            {
                int cx = destX + x;
                if ((uint)cx >= (uint)canvas.Width)
                {
                    continue;
                }

                int from = (y * content.Width + x) * RgbaImage.BytesPerPixel;
                int to = (cy * canvas.Width + cx) * RgbaImage.BytesPerPixel;
                dst[to] = src[from];
                dst[to + 1] = src[from + 1];
                dst[to + 2] = src[from + 2];
                dst[to + 3] = src[from + 3];
            }
        }
    }

    /// <summary>Деление на 2 с округлением к минус бесконечности.</summary>
    private static int FloorDiv2(int value) => value >= 0 ? value / 2 : (value - 1) / 2;

    private enum HorizontalAlign
    {
        Left,
        Center,
        Right,
    }

    private enum VerticalAlign
    {
        Top,
        Middle,
        Bottom,
    }
}
