using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Settings;

namespace Image2Gdram.Core.Presets;

/// <summary>
/// Что делает выбор пресета (решение N-26). «Пользовательский» (<paramref name="preset"/> = <c>null</c>)
/// значения не меняет. На картинке задаются размер, упаковка и схема; на шрифте — только упаковка и схема.
/// </summary>
public static class PresetApplication
{
    public static ImageTabParameters Apply(ImageTabParameters current, Preset? preset)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (preset is null)
        {
            return current with { Preset = PresetBinding.Custom };
        }

        return current with
        {
            Packing = preset.Packing,
            ColorScheme = preset.ColorScheme,
            Processing = current.Processing with
            {
                SizeMode = TargetSizeMode.Manual,
                TargetWidth = preset.Width,
                TargetHeight = preset.Height,
            },
            Preset = PresetBinding.ForPreset(preset),
        };
    }

    public static FontTabParameters Apply(FontTabParameters current, Preset? preset)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (preset is null)
        {
            return current with { Preset = PresetBinding.Custom };
        }

        return current with
        {
            Packing = preset.Packing,
            ColorScheme = preset.ColorScheme,
            Preset = PresetBinding.ForPreset(preset),
        };
    }
}
