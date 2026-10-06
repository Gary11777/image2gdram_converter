using Image2Gdram.Core.Output;

namespace Image2Gdram.Core.Presets;

/// <summary>
/// Какой пресет выбран на вкладке (решение N-26). Обратного узнавания «эти параметры совпали с пресетом» нет:
/// после ручного изменения вызывающий код сам вызывает <see cref="Edited"/>.
/// </summary>
public sealed record PresetBinding
{
    /// <summary>Имя выбранного пресета. <c>null</c> — «Пользовательский».</summary>
    public string? Name { get; init; }

    /// <summary>Имя пресета, от которого параметры изменены вручную. Имеет смысл только при <see cref="Name"/> = <c>null</c>.</summary>
    public string? BasedOn { get; init; }

    public static PresetBinding Custom { get; } = new();

    public bool IsCustom => string.IsNullOrWhiteSpace(Name);

    public static PresetBinding ForPreset(Preset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        return new PresetBinding { Name = preset.Name };
    }

    /// <summary>Ручное изменение параметра, который задаёт пресет: «Пользовательский (на основе …)».</summary>
    public PresetBinding Edited() =>
        string.IsNullOrWhiteSpace(Name) ? this : new PresetBinding { BasedOn = Name.Trim() };

    public PresetInfo ToInfo(Func<string, Preset?>? lookup = null)
    {
        if (!string.IsNullOrWhiteSpace(Name))
        {
            string name = Name.Trim();
            return PresetInfo.Named(name, lookup?.Invoke(name)?.Controller);
        }

        return string.IsNullOrWhiteSpace(BasedOn) ? PresetInfo.Custom : PresetInfo.CustomBasedOn(BasedOn.Trim());
    }
}
