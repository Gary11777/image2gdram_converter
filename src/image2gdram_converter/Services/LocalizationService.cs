using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;

namespace image2gdram_converter.Services;

/// <summary>Чем закончилась загрузка словаря из папки <c>Languages</c>.</summary>
public enum LanguageLoadResult
{
    BuiltIn,
    Loaded,
    NotFound,
    InvalidName,
    Broken,
}

/// <summary>
/// Снимок строк из словарей. Встроенный словарь подключён в <c>App.xaml</c>,
/// дополнительный — из папки <c>Languages</c> рядом с программой.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private static readonly Regex LanguageName = new(@"^[A-Za-z]{2,8}(-[A-Za-z0-9]{1,8})*$", RegexOptions.CultureInvariant);

    private readonly Dictionary<string, string> _text = new(StringComparer.Ordinal);

    public void Add(ResourceDictionary dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        foreach (ResourceDictionary merged in dictionary.MergedDictionaries)
        {
            Add(merged);
        }

        foreach (object key in dictionary.Keys)
        {
            if (key is string name && dictionary[name] is string value)
            {
                _text[name] = value;
            }
        }
    }

    public void Add(IEnumerable<KeyValuePair<string, string>> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        foreach ((string key, string value) in pairs)
        {
            _text[key] = value;
        }
    }

    public void UseApplicationResources()
    {
        if (Application.Current is null)
        {
            return;
        }

        foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
        {
            Add(dictionary);
        }
    }

    /// <summary>
    /// Подключает <c>Languages\&lt;язык&gt;.xaml</c>. <c>ru-RU</c> уже встроен, отдельный файл для него не нужен.
    /// Отсутствующий или испорченный файл не мешает запуску: остаётся встроенный словарь.
    /// </summary>
    public LanguageLoadResult LoadExternal(string? language, string baseDirectory)
    {
        string name = language?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Equals("ru-RU", StringComparison.OrdinalIgnoreCase))
        {
            return LanguageLoadResult.BuiltIn;
        }

        if (!LanguageName.IsMatch(name))
        {
            return LanguageLoadResult.InvalidName;
        }

        string path = Path.Combine(baseDirectory, "Languages", name + ".xaml");
        if (string.IsNullOrWhiteSpace(baseDirectory) || !File.Exists(path))
        {
            return LanguageLoadResult.NotFound;
        }

        ResourceDictionary? dictionary;
        try
        {
            using FileStream stream = File.OpenRead(path);
            dictionary = XamlReader.Load(stream) as ResourceDictionary;
        }
        catch (Exception ex) when (ex is XamlParseException or System.Xml.XmlException or IOException
            or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        {
            return LanguageLoadResult.Broken;
        }

        if (dictionary is null)
        {
            return LanguageLoadResult.Broken;
        }

        Application.Current?.Resources.MergedDictionaries.Add(dictionary);
        Add(dictionary);
        return LanguageLoadResult.Loaded;
    }

    public string Get(string key) => _text.TryGetValue(key, out string? value) ? value : key;

    public string Format(string key, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, Get(key), args);
}
