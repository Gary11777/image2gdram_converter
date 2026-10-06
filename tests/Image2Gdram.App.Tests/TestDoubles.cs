using System.Xml.Linq;
using Image2Gdram.Core.Imaging;
using image2gdram_converter.Services;

namespace Image2Gdram.App.Tests;

internal sealed class MapText : ILocalizationService
{
    private readonly Dictionary<string, string> _text;

    public MapText(IReadOnlyDictionary<string, string> text)
    {
        _text = new Dictionary<string, string>(text, StringComparer.Ordinal);
    }

    public string Get(string key) => _text.TryGetValue(key, out string? value) ? value : key;

    public string Format(string key, params object[] args) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, Get(key), args);
}

internal sealed class FakeDialogs : IDialogService
{
    public bool ConfirmAnswer { get; set; } = true;

    public int ConfirmCount { get; set; }

    public SaveChoice SaveAnswer { get; set; } = SaveChoice.Discard;

    public int SaveCount { get; set; }

    public string? TextAnswer { get; set; }

    public List<string> Alerts { get; } = new();

    public bool Confirm(string message)
    {
        ConfirmCount++;
        return ConfirmAnswer;
    }

    public SaveChoice AskSave(string message)
    {
        SaveCount++;
        return SaveAnswer;
    }

    public string? AskText(string message, string initial) => TextAnswer;

    public void Alert(string message) => Alerts.Add(message);
}

internal sealed class FakeFiles : IFileDialogService
{
    public string? PickOpenImage(string? folder) => null;

    public string? PickOpenProject(string? folder) => null;

    public string? PickSaveProject(string? folder) => null;

    public string? PickFolder(string? folder, string title) => null;
}

internal sealed class FakeClipboard : IClipboardService
{
    public string? Text { get; private set; }

    public void SetText(string text) => Text = text;
}

internal sealed class UnusedDecoder : IImageDecoder
{
    public ImageInfo Identify(string path) => throw new NotSupportedException();

    public DecodedImage Decode(string path, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal static class TestFiles
{
    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "specification.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

    public static Dictionary<string, string> LoadUiStrings()
    {
        string path = Path.Combine(RepoRoot(), "src", "image2gdram_converter", "Resources", "Strings.ru-RU.xaml");
        var document = XDocument.Load(path);
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (XElement element in document.Root!.Elements())
        {
            string? key = element.Attribute(xaml + "Key")?.Value;
            if (key is not null)
            {
                map[key] = element.Value;
            }
        }

        return map;
    }
}
