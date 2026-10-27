namespace Image2Gdram.Core.Processing;

/// <summary>
/// Геометрия прямоугольника фрагмента (N-80). Все формулы шага 3 собраны здесь и покрыты тестами.
/// Прямоугольник задаётся в координатах исходника без поворота — так, как кадр показан на экране.
/// </summary>
public static class CropGeometry
{
    /// <summary>Во сколько раз прямоугольник может быть больше вписанного: <c>k_min = k_fit / 8</c>.</summary>
    public const int MinScaleDivisor = 8;

    /// <summary>
    /// Размер после поворота по часовой стрелке, до отражения. 90° и 270° меняют оси местами (F-07).
    /// </summary>
    public static (int Width, int Height) OrientedSize(int width, int height, Rotation rotation)
    {
        RequireSide(width, nameof(width));
        RequireSide(height, nameof(height));
        return rotation switch
        {
            Rotation.Rotate0 or Rotation.Rotate180 => (width, height),
            Rotation.Rotate90 or Rotation.Rotate270 => (height, width),
            _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "Unknown rotation."),
        };
    }

    /// <summary>
    /// Вписывание: <c>k_fit = min(W / w', H / h')</c>, центр — центр исходника.
    /// <c>w'×h'</c> — размер после поворота.
    /// </summary>
    public static CropViewport Fit(int width, int height, int targetWidth, int targetHeight, Rotation rotation)
    {
        RequireTarget(targetWidth, targetHeight);
        double scale = FitScale(width, height, targetWidth, targetHeight, rotation);
        return new CropViewport(QuantizeCenter(width / 2.0), QuantizeCenter(height / 2.0), QuantizeScale(scale));
    }

    /// <summary><c>k_min = k_fit / 8</c>, <c>k_max = max(k_fit, min(W, H))</c>.</summary>
    public static (double Min, double Max) ScaleRange(int width, int height, int targetWidth, int targetHeight, Rotation rotation)
    {
        RequireTarget(targetWidth, targetHeight);
        double fit = FitScale(width, height, targetWidth, targetHeight, rotation);
        double min = fit / MinScaleDivisor;
        double max = Math.Max(fit, Math.Min(targetWidth, targetHeight));
        return (min, max);
    }

    /// <summary>
    /// Окно фрагмента в ориентированных координатах: <c>span = W/k</c>, начало — <c>ToOriented(центр) − (W/(2k), H/(2k))</c>.
    /// </summary>
    public static CropWindow Window(
        CropViewport viewport,
        int sourceWidth,
        int sourceHeight,
        int targetWidth,
        int targetHeight,
        Rotation rotation,
        bool flipHorizontal,
        bool flipVertical)
    {
        RequireTarget(targetWidth, targetHeight);
        if (!double.IsFinite(viewport.Scale) || viewport.Scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewport), viewport.Scale, "Scale must be finite and positive.");
        }

        (double centerU, double centerV) = ToOriented(
            viewport.CenterX,
            viewport.CenterY,
            sourceWidth,
            sourceHeight,
            rotation,
            flipHorizontal,
            flipVertical);
        double span = targetWidth / viewport.Scale;
        double spanH = targetHeight / viewport.Scale;
        return new CropWindow(
            centerU - targetWidth / (2.0 * viewport.Scale),
            centerV - targetHeight / (2.0 * viewport.Scale),
            span,
            spanH);
    }

    /// <summary>Прямоугольник в координатах исходника. Отражение размер не меняет.</summary>
    public static SourceRect SourceRect(CropViewport viewport, int targetWidth, int targetHeight, Rotation rotation)
    {
        RequireTarget(targetWidth, targetHeight);
        if (!double.IsFinite(viewport.Scale) || viewport.Scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewport), viewport.Scale, "Scale must be finite and positive.");
        }

        bool swapped = rotation is Rotation.Rotate90 or Rotation.Rotate270;
        double rectWidth = (swapped ? targetHeight : targetWidth) / viewport.Scale;
        double rectHeight = (swapped ? targetWidth : targetHeight) / viewport.Scale;
        return new SourceRect(
            viewport.CenterX - rectWidth / 2.0,
            viewport.CenterY - rectHeight / 2.0,
            rectWidth,
            rectHeight);
    }

    /// <summary>
    /// Центр внутри <c>[0, width] × [0, height]</c>, масштаб внутри <see cref="ScaleRange"/>.
    /// Масштаб округляется до 1e-6, центр — до 1e-3.
    /// </summary>
    public static CropViewport Clamp(CropViewport viewport, int width, int height, int targetWidth, int targetHeight, Rotation rotation)
    {
        RequireSide(width, nameof(width));
        RequireSide(height, nameof(height));
        (double min, double max) = ScaleRange(width, height, targetWidth, targetHeight, rotation);
        double scale = viewport.Scale;
        if (!double.IsFinite(scale) || scale <= 0)
        {
            scale = min;
        }

        scale = Math.Min(Math.Max(scale, min), max);
        scale = QuantizeInside(scale, min, max, 6);

        double centerX = double.IsFinite(viewport.CenterX) ? viewport.CenterX : width / 2.0;
        double centerY = double.IsFinite(viewport.CenterY) ? viewport.CenterY : height / 2.0;
        centerX = QuantizeCenterInside(centerX, width);
        centerY = QuantizeCenterInside(centerY, height);
        return new CropViewport(centerX, centerY, scale);
    }

    /// <summary>
    /// Непрерывное отображение точки исходника в координаты после поворота и отражения.
    /// Согласовано с <see cref="RotateFlipStep"/>: сначала поворот по часовой, затем отражение.
    /// Центр пикселя <c>(x+0.5, y+0.5)</c> попадает в тот пиксель, куда шаг 2 кладёт исходный пиксель <c>(x, y)</c>.
    /// </summary>
    public static (double U, double V) ToOriented(
        double u,
        double v,
        int width,
        int height,
        Rotation rotation,
        bool flipHorizontal,
        bool flipVertical)
    {
        (int orientedWidth, int orientedHeight) = OrientedSize(width, height, rotation);
        (double rotatedU, double rotatedV) = rotation switch
        {
            Rotation.Rotate0 => (u, v),
            Rotation.Rotate90 => (height - v, u),
            Rotation.Rotate180 => (width - u, height - v),
            Rotation.Rotate270 => (v, width - u),
            _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "Unknown rotation."),
        };
        if (flipHorizontal)
        {
            rotatedU = orientedWidth - rotatedU;
        }

        if (flipVertical)
        {
            rotatedV = orientedHeight - rotatedV;
        }

        return (rotatedU, rotatedV);
    }

    /// <summary>Обратное к <see cref="ToOriented"/>: точка после поворота и отражения → точка исходника.</summary>
    public static (double U, double V) FromOriented(
        double u,
        double v,
        int width,
        int height,
        Rotation rotation,
        bool flipHorizontal,
        bool flipVertical)
    {
        (int orientedWidth, int orientedHeight) = OrientedSize(width, height, rotation);
        double rotatedU = flipHorizontal ? orientedWidth - u : u;
        double rotatedV = flipVertical ? orientedHeight - v : v;
        return rotation switch
        {
            Rotation.Rotate0 => (rotatedU, rotatedV),
            Rotation.Rotate90 => (rotatedV, height - rotatedU),
            Rotation.Rotate180 => (width - rotatedU, height - rotatedV),
            Rotation.Rotate270 => (width - rotatedV, rotatedU),
            _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "Unknown rotation."),
        };
    }

    /// <summary>
    /// Перевод старых режима вписывания, выравнивания и смещения в прямоугольник (N-80).
    /// «Растянуть» одним масштабом не выражается: берётся <c>k_fit</c>, а центр — точка исходника,
    /// которая в старом алгоритме попадала в центр холста (по осям масштаб свой).
    /// Смещение Y уже в математических осях (плюс — вверх), как после миграции версии 1.
    /// </summary>
    public static CropViewport FromLegacy(
        int sourceWidth,
        int sourceHeight,
        int targetWidth,
        int targetHeight,
        Rotation rotation,
        bool flipHorizontal,
        bool flipVertical,
        FitMode fit,
        Alignment alignment,
        int offsetX,
        int offsetY)
    {
        (int orientedWidth, int orientedHeight) = OrientedSize(sourceWidth, sourceHeight, rotation);
        double uniform = UniformScale(fit, orientedWidth, orientedHeight, targetWidth, targetHeight);
        (int contentWidth, int contentHeight) = LegacyContentSize(
            orientedWidth, orientedHeight, targetWidth, targetHeight, fit);
        (int originX, int originY) = LegacyOrigin(alignment, targetWidth, targetHeight, contentWidth, contentHeight);
        double destX = originX + offsetX;
        double destY = originY - offsetY;
        double scaleX = fit == FitMode.Stretch ? contentWidth / (double)orientedWidth : uniform;
        double scaleY = fit == FitMode.Stretch ? contentHeight / (double)orientedHeight : uniform;
        (double centerX, double centerY) = FromOriented(
            (targetWidth / 2.0 - destX) / scaleX,
            (targetHeight / 2.0 - destY) / scaleY,
            sourceWidth,
            sourceHeight,
            rotation,
            flipHorizontal,
            flipVertical);
        return Clamp(new CropViewport(centerX, centerY, uniform), sourceWidth, sourceHeight, targetWidth, targetHeight, rotation);
    }

    /// <summary>Округление масштаба до 1e-6, половина — от нуля.</summary>
    public static double QuantizeScale(double scale) =>
        Math.Round(scale, 6, MidpointRounding.AwayFromZero);

    /// <summary>Округление координаты центра до 1e-3 пикселя, половина — от нуля.</summary>
    public static double QuantizeCenter(double value) =>
        Math.Round(value, 3, MidpointRounding.AwayFromZero);

    private static double FitScale(int width, int height, int targetWidth, int targetHeight, Rotation rotation)
    {
        (int orientedWidth, int orientedHeight) = OrientedSize(width, height, rotation);
        return Math.Min(targetWidth / (double)orientedWidth, targetHeight / (double)orientedHeight);
    }

    private static double UniformScale(FitMode fit, int orientedWidth, int orientedHeight, int targetWidth, int targetHeight)
    {
        RequireTarget(targetWidth, targetHeight);
        double fitScale = Math.Min(targetWidth / (double)orientedWidth, targetHeight / (double)orientedHeight);
        return fit switch
        {
            FitMode.None => 1d,
            FitMode.Fit => fitScale,
            FitMode.Fill => Math.Max(targetWidth / (double)orientedWidth, targetHeight / (double)orientedHeight),
            FitMode.Stretch => fitScale,
            _ => throw new ArgumentOutOfRangeException(nameof(fit), fit, "Unknown fit mode."),
        };
    }

    private static (int Width, int Height) LegacyContentSize(
        int sourceWidth, int sourceHeight, int targetWidth, int targetHeight, FitMode fit)
    {
        return fit switch
        {
            FitMode.None => (sourceWidth, sourceHeight),
            FitMode.Stretch => (targetWidth, targetHeight),
            FitMode.Fit => ScaleTo(sourceWidth, sourceHeight, targetWidth, targetHeight, useMax: false),
            FitMode.Fill => ScaleTo(sourceWidth, sourceHeight, targetWidth, targetHeight, useMax: true),
            _ => throw new ArgumentOutOfRangeException(nameof(fit), fit, "Unknown fit mode."),
        };
    }

    private static (int Width, int Height) ScaleTo(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight, bool useMax)
    {
        long crossWidth = (long)targetWidth * sourceHeight;
        long crossHeight = (long)targetHeight * sourceWidth;
        bool widthDefines = useMax ? crossWidth >= crossHeight : crossWidth <= crossHeight;
        if (widthDefines)
        {
            int height = Math.Max(1, RoundHalfUp((long)sourceHeight * targetWidth, sourceWidth));
            return (targetWidth, height);
        }

        int width = Math.Max(1, RoundHalfUp((long)sourceWidth * targetHeight, sourceHeight));
        return (width, targetHeight);
    }

    private static int RoundHalfUp(long numer, long denom) => (int)((numer + denom / 2) / denom);

    private static (int X, int Y) LegacyOrigin(Alignment alignment, int canvasWidth, int canvasHeight, int contentWidth, int contentHeight)
    {
        (int horizontal, int vertical) = alignment switch
        {
            Alignment.TopLeft => (0, 0),
            Alignment.TopCenter => (1, 0),
            Alignment.TopRight => (2, 0),
            Alignment.MiddleLeft => (0, 1),
            Alignment.Center => (1, 1),
            Alignment.MiddleRight => (2, 1),
            Alignment.BottomLeft => (0, 2),
            Alignment.BottomCenter => (1, 2),
            Alignment.BottomRight => (2, 2),
            _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, "Unknown alignment."),
        };
        int x = horizontal switch
        {
            0 => 0,
            1 => FloorDiv2(canvasWidth - contentWidth),
            _ => canvasWidth - contentWidth,
        };
        int y = vertical switch
        {
            0 => 0,
            1 => FloorDiv2(canvasHeight - contentHeight),
            _ => canvasHeight - contentHeight,
        };
        return (x, y);
    }

    private static int FloorDiv2(int value) => value >= 0 ? value / 2 : (value - 1) / 2;

    private static double QuantizeInside(double value, double min, double max, int digits)
    {
        double quantized = Math.Round(value, digits, MidpointRounding.AwayFromZero);
        double unit = Math.Pow(10, -digits);
        if (quantized < min)
        {
            quantized = Math.Ceiling(min / unit) * unit;
        }

        if (quantized > max)
        {
            quantized = Math.Floor(max / unit) * unit;
        }

        if (quantized < min)
        {
            quantized = min;
        }

        if (quantized > max)
        {
            quantized = max;
        }

        return quantized;
    }

    private static double QuantizeCenterInside(double value, int limit)
    {
        double quantized = QuantizeCenter(Math.Min(Math.Max(value, 0), limit));
        if (quantized < 0)
        {
            return 0;
        }

        if (quantized > limit)
        {
            return limit;
        }

        return quantized;
    }

    private static void RequireSide(int value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, name);
    }

    private static void RequireTarget(int targetWidth, int targetHeight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(targetWidth, 1, nameof(targetWidth));
        ArgumentOutOfRangeException.ThrowIfLessThan(targetHeight, 1, nameof(targetHeight));
    }
}
