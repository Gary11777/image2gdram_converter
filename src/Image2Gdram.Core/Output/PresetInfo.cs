namespace Image2Gdram.Core.Output;

/// <summary>
/// Пресет в том виде, в каком он печатается в заголовке-комментарии (решение N-02):
/// «OLED128X64-0.96 (SSD1306)», «Пользовательский» или «Пользовательский (на основе OLED128X64-0.96)».
/// </summary>
public sealed record PresetInfo
{
    private PresetInfo(string? name, string? controller, string? basedOn)
    {
        Name = name;
        Controller = controller;
        BasedOn = basedOn;
    }

    public static PresetInfo Custom { get; } = new(null, null, null);

    /// <summary>Имя пресета; <c>null</c> — «Пользовательский».</summary>
    public string? Name { get; }

    public string? Controller { get; }

    /// <summary>Имя пресета, от которого изменены параметры.</summary>
    public string? BasedOn { get; }

    public bool IsCustom => Name is null;

    public static PresetInfo Named(string name, string? controller = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new PresetInfo(name, string.IsNullOrWhiteSpace(controller) ? null : controller, null);
    }

    public static PresetInfo CustomBasedOn(string presetName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(presetName);
        return new PresetInfo(null, null, presetName);
    }
}
