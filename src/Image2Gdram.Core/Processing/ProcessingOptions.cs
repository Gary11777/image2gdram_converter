namespace Image2Gdram.Core.Processing;

/// <summary>
/// Параметры шагов 1–5 (п. 4.1.2 ТЗ). Значения по умолчанию — N-41 и N-80:
/// белый фон, без поворота, размер 128×64, фрагмент вписан (<see cref="Crop"/> = <c>null</c>),
/// усреднение по площади, порог 128.
/// </summary>
public sealed record ProcessingOptions
{
    public const int MinTargetSide = 1;
    public const int MaxTargetSide = 1024;
    public const int DefaultThreshold = 128;

    public static ProcessingOptions Default { get; } = new();

    public BackgroundColor Background { get; init; } = BackgroundColor.White;

    public Rotation Rotation { get; init; } = Rotation.Rotate0;

    public bool FlipHorizontal { get; init; }

    public bool FlipVertical { get; init; }

    public int TargetWidth { get; init; } = 128;

    public int TargetHeight { get; init; } = 64;

    /// <summary>
    /// Фрагмент исходника. <c>null</c> — вписать автоматически (<see cref="CropGeometry.Fit"/>):
    /// так при старте без изображения и для файлов, где прежнее «вписать по центру» ничем не отличалось.
    /// </summary>
    public CropViewport? Crop { get; init; }

    public ResampleMode Resample { get; init; } = ResampleMode.AreaAverage;

    public BinarizeMode Binarize { get; init; } = BinarizeMode.Threshold;

    public int Threshold { get; init; } = DefaultThreshold;
}
