using System.Globalization;

namespace image2gdram_converter;

/// <summary>Масштаб преобразования: показ до трёх знаков и ввод с точкой или запятой (N-80).</summary>
public static class ScaleFormat
{
    public const double StepFactor = 1.1;

    public static double LogStep { get; } = Math.Log2(StepFactor);

    public static bool TryParse(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string normalized = text.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value)
            && value > 0;
    }

    public static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    public static string FormatBound(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    public static string FormatDim(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
