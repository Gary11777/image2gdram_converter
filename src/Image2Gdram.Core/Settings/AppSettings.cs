using Image2Gdram.Core.Presets;

namespace Image2Gdram.Core.Settings;

/// <summary>Последние папки диалогов. Пустая строка хранится как отсутствие папки.</summary>
public sealed record LastFolders
{
    public string? OpenImage { get; init; }

    public string? OpenFont { get; init; }

    public string? SaveOutput { get; init; }

    public string? OpenProject { get; init; }

    public string? SaveProject { get; init; }
}

/// <summary>
/// Последние параметры программы (п. 4.5 ТЗ, решение N-25). Исходные файлы сюда записываются как пути,
/// но при запуске не открываются. Ручные правки и таблица шрифта живут в проекте <c>.iiu</c>, не здесь.
/// </summary>
public sealed class AppSettings
{
    public const int FormatVersion = 1;
    public const string DefaultLanguage = "ru-RU";
    public const int MaxRecentFiles = 10;

    public AppSettings()
        : this(new UserPresetStore(PresetCatalog.Shared))
    {
    }

    public AppSettings(UserPresetStore userPresets)
    {
        ArgumentNullException.ThrowIfNull(userPresets);
        UserPresets = userPresets;
    }

    /// <summary>Имя словаря интерфейса, например <c>ru-RU</c>. Не проверяется по установленным языкам.</summary>
    public string Language { get; set; } = DefaultLanguage;

    public ImageSettingsTab Image { get; set; } = ImageSettingsTab.CreateDefault();

    public FontSettingsTab Font { get; set; } = FontSettingsTab.CreateDefault();

    public UserPresetStore UserPresets { get; }

    /// <summary>Недавние файлы: новые в начале, без повторов без учёта регистра, не больше <see cref="MaxRecentFiles"/>.</summary>
    public List<string> RecentFiles { get; } = new();

    public LastFolders Folders { get; set; } = new();

    public static AppSettings CreateDefault() => new();

    public void RememberRecent(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string full = Path.GetFullPath(path);
        RecentFiles.RemoveAll(item => string.Equals(item, full, StringComparison.OrdinalIgnoreCase));
        RecentFiles.Insert(0, full);
        if (RecentFiles.Count > MaxRecentFiles)
        {
            RecentFiles.RemoveRange(MaxRecentFiles, RecentFiles.Count - MaxRecentFiles);
        }
    }
}
