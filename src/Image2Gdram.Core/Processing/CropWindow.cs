namespace Image2Gdram.Core.Processing;

/// <summary>
/// Окно шага 3 в координатах кадра после поворота и отражения (N-80).
/// <see cref="Span"/> = W/k, <see cref="SpanH"/> = H/k.
/// </summary>
public readonly record struct CropWindow(double OriginX, double OriginY, double Span, double SpanH);
