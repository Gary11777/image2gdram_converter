namespace Image2Gdram.Core.Processing;

/// <summary>
/// Параметры шагов 1–5 (п. 4.1.2 ТЗ). Значения по умолчанию — решение N-41:
/// белый фон, без поворота, ручной размер 128×64, вписывание, центр, усреднение, порог 128.
/// </summary>
public sealed record ProcessingOptions
{
    public const int MinOffset = -1024;
    public const int MaxOffset = 1024;
    public const int MinTargetSide = 1;
    public const int MaxTargetSide = 1024;
    public const int DefaultThreshold = 128;

    public static ProcessingOptions Default { get; } = new();

    public BackgroundColor Background { get; init; } = BackgroundColor.White;

    public Rotation Rotation { get; init; } = Rotation.Rotate0;

    public bool FlipHorizontal { get; init; }

    public bool FlipVertical { get; init; }

    public TargetSizeMode SizeMode { get; init; } = TargetSizeMode.Manual;

    public int TargetWidth { get; init; } = 128;

    public int TargetHeight { get; init; } = 64;

    public FitMode Fit { get; init; } = FitMode.Fit;

    public Alignment Alignment { get; init; } = Alignment.Center;

    public int OffsetX { get; init; }

    public int OffsetY { get; init; }

    public ResampleMode Resample { get; init; } = ResampleMode.AreaAverage;

    public BinarizeMode Binarize { get; init; } = BinarizeMode.Threshold;

    public int Threshold { get; init; } = DefaultThreshold;
}
