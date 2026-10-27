namespace Image2Gdram.Core.Processing;

/// <summary>
/// Параметры вписывания из файла версии 1 или 2, которые ещё нельзя перевести в <see cref="CropViewport"/>:
/// для этого нужен размер исходника (N-80). После загрузки изображения их заменяет <c>CropGeometry.FromLegacy</c>.
/// </summary>
public sealed record LegacyPlacement(
    TargetSizeMode SizeMode,
    FitMode Fit,
    Alignment Alignment,
    int OffsetX,
    int OffsetY)
{
    /// <summary>Абсолютная граница смещения в файлах версий 1 и 2. В интерфейсе картинки смещения больше нет.</summary>
    public const int MinOffset = -1024;

    /// <summary>Абсолютная граница смещения в файлах версий 1 и 2.</summary>
    public const int MaxOffset = 1024;
}
