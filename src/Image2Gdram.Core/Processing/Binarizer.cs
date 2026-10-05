namespace Image2Gdram.Core.Processing;

/// <summary>Выбор бинаризатора по режиму п. 4.1.4.</summary>
public static class Binarizer
{
    public static IBinarizer Create(BinarizeMode mode) => mode switch
    {
        BinarizeMode.Threshold => new ThresholdBinarizer(),
        BinarizeMode.FloydSteinberg => new FloydSteinbergDitherer(),
        BinarizeMode.Atkinson => new AtkinsonDitherer(),
        BinarizeMode.Bayer4 => new BayerDitherer(4),
        BinarizeMode.Bayer8 => new BayerDitherer(8),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown binarize mode."),
    };
}
