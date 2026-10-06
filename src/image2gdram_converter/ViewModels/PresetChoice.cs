using Image2Gdram.Core.Presets;

namespace image2gdram_converter.ViewModels;

public sealed class PresetChoice
{
    public PresetChoice(Preset? preset, string caption, bool isUser, bool isCustom)
    {
        Preset = preset;
        Caption = caption;
        IsUser = isUser;
        IsCustom = isCustom;
    }

    public Preset? Preset { get; }

    public string Caption { get; }

    public bool IsUser { get; }

    public bool IsCustom { get; }
}
