using Image2Gdram.Core.Imaging;

namespace Image2Gdram.Core.Processing;

/// <summary>
/// Шаг 3: фрагмент ориентированного кадра переносится в целевой размер (N-80, D-06).
/// Выходной пиксель <c>(x, y)</c> покрывает окно
/// <c>[ox + x·span/W, ox + (x+1)·span/W) × [oy + y·spanH/H, oy + (y+1)·spanH/H)</c>,
/// где <c>span = W/k</c> и <c>spanH = H/k</c>. При <c>ox = 0</c> и <c>span</c>, равном ширине исходника,
/// окно совпадает с прежним усреднением по площади.
/// </summary>
public static class ResizeStep
{
    public static RgbaImage Apply(
        RgbaImage oriented,
        int targetWidth,
        int targetHeight,
        double originX,
        double originY,
        double span,
        ResampleMode resample,
        BackgroundColor background,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(oriented);
        ArgumentOutOfRangeException.ThrowIfLessThan(targetWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(targetHeight, 1);
        if (!double.IsFinite(originX) || !double.IsFinite(originY) || !double.IsFinite(span) || span <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(span), span, "Origin and span must be finite, span positive.");
        }

        double spanH = span * targetHeight / targetWidth;
        return resample switch
        {
            ResampleMode.NearestNeighbor => Nearest(oriented, targetWidth, targetHeight, originX, originY, span, spanH, background, cancellationToken),
            ResampleMode.AreaAverage => AreaAverage(oriented, targetWidth, targetHeight, originX, originY, span, spanH, background, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(resample), resample, "Unknown resample mode."),
        };
    }

    private static RgbaImage Nearest(
        RgbaImage source,
        int width,
        int height,
        double originX,
        double originY,
        double span,
        double spanH,
        BackgroundColor background,
        CancellationToken cancellationToken)
    {
        byte bg = BackgroundCompositor.BackgroundValue(background);
        var destination = new RgbaImage(width, height);
        byte[] dst = destination.PixelBuffer;
        int srcW = source.Width;
        int srcH = source.Height;
        for (int y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int sy = (int)Math.Floor(originY + (y + 0.5) * spanH / height);
            for (int x = 0; x < width; x++)
            {
                int sx = (int)Math.Floor(originX + (x + 0.5) * span / width);
                int i = (y * width + x) * RgbaImage.BytesPerPixel;
                if ((uint)sx >= (uint)srcW || (uint)sy >= (uint)srcH)
                {
                    dst[i] = bg;
                    dst[i + 1] = bg;
                    dst[i + 2] = bg;
                    dst[i + 3] = 255;
                    continue;
                }

                source.GetPixel(sx, sy, out byte r, out byte g, out byte b, out byte a);
                dst[i] = r;
                dst[i + 1] = g;
                dst[i + 2] = b;
                dst[i + 3] = a;
            }
        }

        return destination;
    }

    private static RgbaImage AreaAverage(
        RgbaImage source,
        int width,
        int height,
        double originX,
        double originY,
        double span,
        double spanH,
        BackgroundColor background,
        CancellationToken cancellationToken)
    {
        byte bg = BackgroundCompositor.BackgroundValue(background);
        var destination = new RgbaImage(width, height);
        byte[] dst = destination.PixelBuffer;
        int srcW = source.Width;
        int srcH = source.Height;
        ReadOnlySpan<byte> src = source.Pixels;
        for (int y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double y0 = originY + y * spanH / height;
            double y1 = originY + (y + 1) * spanH / height;
            for (int x = 0; x < width; x++)
            {
                double x0 = originX + x * span / width;
                double x1 = originX + (x + 1) * span / width;
                double windowArea = (x1 - x0) * (y1 - y0);
                int o = (y * width + x) * RgbaImage.BytesPerPixel;
                if (windowArea <= 0)
                {
                    dst[o] = bg;
                    dst[o + 1] = bg;
                    dst[o + 2] = bg;
                    dst[o + 3] = 255;
                    continue;
                }

                int sx0 = (int)Math.Floor(x0);
                int sx1 = (int)Math.Ceiling(x1) - 1;
                int sy0 = (int)Math.Floor(y0);
                int sy1 = (int)Math.Ceiling(y1) - 1;
                if (sx0 < 0)
                {
                    sx0 = 0;
                }

                if (sy0 < 0)
                {
                    sy0 = 0;
                }

                if (sx1 >= srcW)
                {
                    sx1 = srcW - 1;
                }

                if (sy1 >= srcH)
                {
                    sy1 = srcH - 1;
                }

                double sumR = 0;
                double sumG = 0;
                double sumB = 0;
                double sumA = 0;
                double inside = 0;
                if (sx0 <= sx1 && sy0 <= sy1)
                {
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
                            double weight = Overlap(sx, sx + 1, x0, x1) * wy;
                            if (weight <= 0)
                            {
                                continue;
                            }

                            int i = row + sx * RgbaImage.BytesPerPixel;
                            sumR += src[i] * weight;
                            sumG += src[i + 1] * weight;
                            sumB += src[i + 2] * weight;
                            sumA += src[i + 3] * weight;
                            inside += weight;
                        }
                    }
                }

                double outside = windowArea - inside;
                if (outside < 0)
                {
                    outside = 0;
                }

                sumR += bg * outside;
                sumG += bg * outside;
                sumB += bg * outside;
                sumA += 255d * outside;
                dst[o] = RoundChannel(sumR, windowArea);
                dst[o + 1] = RoundChannel(sumG, windowArea);
                dst[o + 2] = RoundChannel(sumB, windowArea);
                dst[o + 3] = RoundChannel(sumA, windowArea);
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
}
