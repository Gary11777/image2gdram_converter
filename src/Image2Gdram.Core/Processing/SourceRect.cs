namespace Image2Gdram.Core.Processing;

/// <summary>Прямоугольник фрагмента в координатах исходника: левый верхний угол и размер (N-80).</summary>
public readonly record struct SourceRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;

    public double Bottom => Y + Height;
}
