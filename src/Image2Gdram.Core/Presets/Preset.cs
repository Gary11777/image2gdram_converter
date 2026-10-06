using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Presets;

/// <summary>
/// Пресет дисплея: размер целевого изображения, упаковка и цветовая схема (приложение А, решение D-01).
/// На вкладке шрифтов размер не применяется — его задаёт ячейка (решение D-05).
/// </summary>
public sealed class Preset
{
    public const int MaxNameLength = 80;
    public const int MaxControllerLength = 80;

    public Preset(
        string name,
        string? controller,
        int width,
        int height,
        PackingOptions packing,
        ColorScheme colorScheme,
        bool isBuiltIn)
    {
        ArgumentNullException.ThrowIfNull(packing);
        Name = NormalizeName(name);
        Controller = NormalizeController(controller);
        if (width is < 1 or > 1024 || height is < 1 or > 1024)
        {
            throw new PresetException(PresetError.InvalidSize, Name);
        }

        if (!Enum.IsDefined(colorScheme))
        {
            throw new ArgumentOutOfRangeException(nameof(colorScheme), colorScheme, "Unknown color scheme.");
        }

        EnsurePacking(packing, Name);
        Width = width;
        Height = height;
        Packing = packing;
        ColorScheme = colorScheme;
        IsBuiltIn = isBuiltIn;
    }

    public string Name { get; }

    public string? Controller { get; }

    public int Width { get; }

    public int Height { get; }

    public PackingOptions Packing { get; }

    public ColorScheme ColorScheme { get; }

    public bool IsBuiltIn { get; }

    public Preset WithName(string name) =>
        new(name, Controller, Width, Height, Packing, ColorScheme, IsBuiltIn);

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new PresetException(PresetError.EmptyName, name);
        }

        string trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength || trimmed.Any(char.IsControl))
        {
            throw new PresetException(PresetError.InvalidName, trimmed);
        }

        return trimmed;
    }

    private static string? NormalizeController(string? controller)
    {
        if (string.IsNullOrWhiteSpace(controller))
        {
            return null;
        }

        string trimmed = controller.Trim();
        if (trimmed.Length > MaxControllerLength || trimmed.Any(char.IsControl))
        {
            throw new PresetException(PresetError.InvalidController, trimmed);
        }

        return trimmed;
    }

    private static void EnsurePacking(PackingOptions packing, string name)
    {
        if (!Enum.IsDefined(packing.PixelFormat) || !Enum.IsDefined(packing.Direction)
            || !Enum.IsDefined(packing.BitOrder) || !Enum.IsDefined(packing.PageTraversal)
            || !Enum.IsDefined(packing.ByteOrder) || packing.BitsPerByte is not (6 or 8))
        {
            throw new PresetException(PresetError.InvalidPacking, name);
        }
    }
}
