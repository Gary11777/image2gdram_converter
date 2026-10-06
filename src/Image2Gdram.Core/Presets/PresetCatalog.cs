using System.Reflection;
using Image2Gdram.Core.Persistence;

namespace Image2Gdram.Core.Presets;

/// <summary>
/// Встроенные пресеты приложения А (решение D-01). Данные лежат в одном ресурсе <c>presets.json</c>,
/// а не в коде. «Пользовательский» в каталог не входит и параметров не задаёт.
/// </summary>
public sealed class PresetCatalog
{
    public const string ResourceName = "Image2Gdram.Core.Presets.presets.json";
    public const string CustomName = "Пользовательский";
    public const int FormatVersion = 1;

    private readonly Preset[] _builtIn;

    private PresetCatalog(Preset[] builtIn)
    {
        _builtIn = builtIn;
    }

    public static PresetCatalog Shared { get; } = Load();

    public IReadOnlyList<Preset> BuiltIn => _builtIn;

    public Preset? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        string trimmed = name.Trim();
        foreach (Preset preset in _builtIn)
        {
            if (string.Equals(preset.Name, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return preset;
            }
        }

        return null;
    }

    public bool IsBuiltInName(string? name) => Find(name) is not null;

    public bool IsReservedName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && string.Equals(name.Trim(), CustomName, StringComparison.OrdinalIgnoreCase);

    private static PresetCatalog Load()
    {
        Assembly assembly = typeof(PresetCatalog).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Built-in preset resource '{ResourceName}' is missing.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        PresetFileDto dto;
        try
        {
            dto = JsonFormat.Parse<PresetFileDto>(memory.ToArray());
        }
        catch (Exception ex) when (ex is FormatException or StoredDataException)
        {
            throw new InvalidOperationException("Built-in preset catalog is not valid JSON.", ex);
        }

        if (dto.FormatVersion != FormatVersion || dto.Presets is null || dto.Presets.Count == 0)
        {
            throw new InvalidOperationException("Built-in preset catalog has an unsupported version or is empty.");
        }

        var presets = new Preset[dto.Presets.Count];
        for (int i = 0; i < dto.Presets.Count; i++)
        {
            try
            {
                presets[i] = PresetMapping.ToPreset(dto.Presets[i], isBuiltIn: true);
            }
            catch (Exception ex) when (ex is PresetException or StoredDataException or ArgumentException)
            {
                throw new InvalidOperationException($"Built-in preset at index {i} is invalid.", ex);
            }

            if (string.Equals(presets[i].Name, CustomName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The built-in catalog must not contain the custom preset.");
            }

            for (int j = 0; j < i; j++)
            {
                if (string.Equals(presets[j].Name, presets[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Duplicate built-in preset '{presets[i].Name}'.");
                }
            }
        }

        return new PresetCatalog(presets);
    }
}
