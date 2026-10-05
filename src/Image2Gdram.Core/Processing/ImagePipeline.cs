using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Processing;

/// <summary>
/// Шаги 1–7 п. 4.1.2: фон, поворот и отражение, размер, серое, бинаризация, правки, упаковка.
/// Один экземпляр кэширует промежуточные кадры и не рассчитан на параллельные вызовы.
/// Смена порога или режима бинаризации не повторяет масштабирование.
/// </summary>
public sealed class ImagePipeline
{
    private readonly PackerRegistry _packers;

    private RgbaImage? _source;
    private BackgroundColor _background;
    private RgbaImage? _composited;
    private Rotation _rotation;
    private bool _flipHorizontal;
    private bool _flipVertical;
    private RgbaImage? _oriented;
    private ResizeKey _resizeKey;
    private RgbaImage? _resized;
    private GrayImage? _gray;

    public ImagePipeline(PackerRegistry? packers = null)
    {
        _packers = packers ?? PackerRegistry.CreateDefault();
    }

    /// <summary>Шаги 1–6. <paramref name="overrides"/> накладываются после бинаризации.</summary>
    public MonoBitmap Run(
        RgbaImage source,
        ProcessingOptions options,
        PixelOverrides? overrides = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);
        cancellationToken.ThrowIfCancellationRequested();

        RgbaImage oriented = Orient(source, options, cancellationToken);
        (int targetWidth, int targetHeight) = TargetSize(oriented, options);
        RgbaImage resized = Resize(oriented, options, targetWidth, targetHeight, cancellationToken);
        GrayImage gray = Grayscale(resized);
        cancellationToken.ThrowIfCancellationRequested();

        MonoBitmap bitmap = Binarizer.Create(options.Binarize).Apply(gray, options.Threshold);
        overrides?.Apply(bitmap);
        return bitmap;
    }

    /// <summary>Шаг 7: инверсия и упаковка уже посчитанного растра.</summary>
    public byte[] Pack(MonoBitmap bitmap, PackingOptions packing)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(packing);
        return _packers.Get(packing).Pack(bitmap, packing);
    }

    /// <summary>Шаги 1–7 подряд.</summary>
    public byte[] RunAndPack(
        RgbaImage source,
        ProcessingOptions options,
        PackingOptions packing,
        PixelOverrides? overrides = null,
        CancellationToken cancellationToken = default)
    {
        MonoBitmap bitmap = Run(source, options, overrides, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return Pack(bitmap, packing);
    }

    private RgbaImage Orient(RgbaImage source, ProcessingOptions options, CancellationToken cancellationToken)
    {
        if (_composited is null || !ReferenceEquals(_source, source) || _background != options.Background)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _source = source;
            _background = options.Background;
            _composited = BackgroundCompositor.Apply(source, options.Background);
            _oriented = null;
            _resized = null;
            _gray = null;
        }

        if (_oriented is null
            || _rotation != options.Rotation
            || _flipHorizontal != options.FlipHorizontal
            || _flipVertical != options.FlipVertical)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _rotation = options.Rotation;
            _flipHorizontal = options.FlipHorizontal;
            _flipVertical = options.FlipVertical;
            _oriented = RotateFlipStep.Apply(_composited, options.Rotation, options.FlipHorizontal, options.FlipVertical);
            _resized = null;
            _gray = null;
        }

        return _oriented;
    }

    private RgbaImage Resize(RgbaImage oriented, ProcessingOptions options, int targetWidth, int targetHeight, CancellationToken cancellationToken)
    {
        var key = new ResizeKey(
            targetWidth,
            targetHeight,
            options.Fit,
            options.Alignment,
            options.OffsetX,
            options.OffsetY,
            options.Resample,
            options.Background);
        if (_resized is null || _resizeKey != key)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _resizeKey = key;
            _resized = ResizeStep.Apply(
                oriented,
                targetWidth,
                targetHeight,
                options.Fit,
                options.Alignment,
                options.OffsetX,
                options.OffsetY,
                options.Resample,
                options.Background,
                cancellationToken);
            _gray = null;
        }

        return _resized;
    }

    private GrayImage Grayscale(RgbaImage resized)
    {
        _gray ??= GrayscaleStep.Apply(resized);
        return _gray;
    }

    private static (int Width, int Height) TargetSize(RgbaImage oriented, ProcessingOptions options)
    {
        if (options.SizeMode == TargetSizeMode.MatchSource)
        {
            if (oriented.Width > ProcessingOptions.MaxTargetSide || oriented.Height > ProcessingOptions.MaxTargetSide)
            {
                throw new PipelineException(PipelineErrorCode.SourceLargerThan1024, oriented.Width, oriented.Height);
            }

            return (oriented.Width, oriented.Height);
        }

        return (options.TargetWidth, options.TargetHeight);
    }

    private static void Validate(ProcessingOptions options)
    {
        if (!Enum.IsDefined(options.Background))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Background, "Unknown background.");
        }

        if (!Enum.IsDefined(options.Rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Rotation, "Unknown rotation.");
        }

        if (!Enum.IsDefined(options.SizeMode))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.SizeMode, "Unknown size mode.");
        }

        if (!Enum.IsDefined(options.Fit))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Fit, "Unknown fit mode.");
        }

        if (!Enum.IsDefined(options.Alignment))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Alignment, "Unknown alignment.");
        }

        if (!Enum.IsDefined(options.Resample))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Resample, "Unknown resample mode.");
        }

        if (!Enum.IsDefined(options.Binarize))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Binarize, "Unknown binarize mode.");
        }

        if (options.Threshold is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Threshold, "Threshold must be in 0..255.");
        }

        if (options.OffsetX is < ProcessingOptions.MinOffset or > ProcessingOptions.MaxOffset)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.OffsetX, "Offset X must be in -1024..1024.");
        }

        if (options.OffsetY is < ProcessingOptions.MinOffset or > ProcessingOptions.MaxOffset)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.OffsetY, "Offset Y must be in -1024..1024.");
        }

        if (options.SizeMode == TargetSizeMode.Manual)
        {
            if (options.TargetWidth is < ProcessingOptions.MinTargetSide or > ProcessingOptions.MaxTargetSide)
            {
                throw new ArgumentOutOfRangeException(nameof(options), options.TargetWidth, "Width must be in 1..1024.");
            }

            if (options.TargetHeight is < ProcessingOptions.MinTargetSide or > ProcessingOptions.MaxTargetSide)
            {
                throw new ArgumentOutOfRangeException(nameof(options), options.TargetHeight, "Height must be in 1..1024.");
            }
        }
    }

    private readonly record struct ResizeKey(
        int TargetWidth,
        int TargetHeight,
        FitMode Fit,
        Alignment Alignment,
        int OffsetX,
        int OffsetY,
        ResampleMode Resample,
        BackgroundColor Background);
}
