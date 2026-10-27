namespace Image2Gdram.Core.Processing;

/// <summary>
/// Фрагмент исходного кадра (N-80): центр в пикселях исходника без поворота и масштаб преобразования.
/// Масштаб — число выходных пикселей на один пиксель исходника, по обеим осям одинаковый.
/// </summary>
public readonly record struct CropViewport(double CenterX, double CenterY, double Scale);
