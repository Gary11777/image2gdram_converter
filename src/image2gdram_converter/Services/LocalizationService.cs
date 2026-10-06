using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace image2gdram_converter.Services;

/// <summary>
/// Снимок строк из словарей. Встроенный словарь подключён в <c>App.xaml</c>,
/// дополнительный — из папки <c>Languages</c> рядом с программой.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private readonly Dictionary<string, string> _text = new(StringComparer.Ordinal);

    public void Add(ResourceDictionary dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
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
    /// Подключает <c>Languages\&lt;язык&gt;.xaml</c>, если файл есть.
    /// <c>ru-RU</c> уже встроен, отдельный файл для него не нужен.
    /// </summary>
    public bool TryLoadExternal(string? language, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(language)
            || language.Equals("ru-RU", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(baseDirectory))
        {
            return false;
        }

        string path = Path.Combine(baseDirectory, "Languages", language.Trim() + ".xaml");
        if (!File.Exists(path))
        {
            return false;
        }

        using FileStream stream = File.OpenRead(path);
        if (XamlReader.Load(stream) is not ResourceDictionary dictionary)
        {
            return false;
        }

        Application.Current?.Resources.MergedDictionaries.Add(dictionary);
        Add(dictionary);
        return true;
    }

    public string Get(string key) => _text.TryGetValue(key, out string? value) ? value : key;

    public string Format(string key, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, Get(key), args);
}
