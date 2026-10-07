using System.Windows;

namespace image2gdram_converter;

/// <summary>
/// Минимальный и текущий размер окна с учётом рабочей области монитора (решение N-57).
/// Минимум ТЗ — 1024×680 единиц WPF; если при крупном масштабе Windows рабочая область меньше,
/// минимум и размер уменьшаются до неё, а панели параметров и кода прокручиваются.
/// </summary>
public static class WindowFit
{
    public const double MinWidth = 1024;
    public const double MinHeight = 680;

    public static (Size Minimum, Size Size) Fit(Size current, Rect workArea)
    {
        if (workArea.Width < 1 || workArea.Height < 1)
        {
            return (new Size(MinWidth, MinHeight), current);
        }

        var minimum = new Size(Math.Min(MinWidth, workArea.Width), Math.Min(MinHeight, workArea.Height));
        var size = new Size(
            Math.Clamp(double.IsFinite(current.Width) ? current.Width : minimum.Width, minimum.Width, workArea.Width),
            Math.Clamp(double.IsFinite(current.Height) ? current.Height : minimum.Height, minimum.Height, workArea.Height));
        return (minimum, size);
    }
}
