using System.Globalization;

namespace image2gdram_converter;

/// <summary>Разбор целого поля в инвариантной культуре.</summary>
public static class IntText
{
    public static bool TryParse(string? text, int min, int max, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
            && value >= min
            && value <= max;
    }

    public static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
}
