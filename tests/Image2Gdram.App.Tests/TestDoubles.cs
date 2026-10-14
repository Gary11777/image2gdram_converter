using System.Xml.Linq;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Fonts.Import;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Packing;
using image2gdram_converter;
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

    public ImportPick? ImportAnswer { get; set; }

    public int ImportAsks { get; set; }

    public ImportPick? AskImport(IReadOnlyList<ImportedArray> arrays, FontCellSize cell, PackingOptions packing)
    {
        ImportAsks++;
        return ImportAnswer;
    }
}

internal sealed class FakeFiles : IFileDialogService
{
    public string? NextImage { get; set; }

    public string? NextSheet { get; set; }

    public string? NextImport { get; set; }

    public string? NextOpenProject { get; set; }

    public string? NextSaveProject { get; set; }

    public string? NextSaveOutput { get; set; }

    public Queue<string?> NextSaveOutputs { get; } = new();

    public string? LastFolder { get; private set; }

    public string? LastFileName { get; private set; }

    public string? LastTitle { get; private set; }

    public string? LastFilter { get; private set; }

    public string? LastDefaultExt { get; private set; }

    public int SaveOutputCalls { get; private set; }

    public string? PickOpenImage(string? folder) => NextImage;

    public string? PickOpenSheet(string? folder) => NextSheet;

    public string? PickOpenImport(string? folder) => NextImport;

    public string? PickOpenProject(string? folder) => NextOpenProject;

    public string? PickSaveProject(string? folder) => NextSaveProject;

    public string? PickSaveOutput(string? folder, string fileName, string title, string filter, string defaultExt)
    {
        SaveOutputCalls++;
        LastFolder = folder;
        LastFileName = fileName;
        LastTitle = title;
        LastFilter = filter;
        LastDefaultExt = defaultExt;
        return NextSaveOutputs.Count > 0 ? NextSaveOutputs.Dequeue() : NextSaveOutput;
    }
}

internal sealed class DeferredRecalcScheduler : IRecalcScheduler
{
    private readonly List<(Func<CancellationToken, object?> Compute, Action<object?> Apply, Action<Exception> OnError)> _pending = new();

    public bool Hold { get; set; }

    public int PendingCount => _pending.Count;

    public void Schedule(Func<CancellationToken, object?> compute, Action<object?> apply, Action<Exception> onError)
    {
        if (Hold)
        {
            _pending.Add((compute, apply, onError));
            return;
        }

        try
        {
            apply(compute(CancellationToken.None));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            onError(ex);
        }
    }

    public void RunPending()
    {
        var items = _pending.ToArray();
        _pending.Clear();
        foreach (var item in items)
        {
            item.Apply(item.Compute(CancellationToken.None));
        }
    }
}

/// <summary>Декодер, который отдаёт заранее заданные изображения по пути.</summary>
internal sealed class MapDecoder : IImageDecoder
{
    private readonly Dictionary<string, DecodedImage> _images = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string path, DecodedImage image) => _images[Path.GetFullPath(path)] = image;

    public ImageInfo Identify(string path)
    {
        DecodedImage image = Decode(path);
        return new ImageInfo(image.Format, image.Width, image.Height, image.Frames.Count);
    }

    public DecodedImage Decode(string path, CancellationToken cancellationToken = default) =>
        _images.TryGetValue(Path.GetFullPath(path), out DecodedImage? image)
            ? image
            : throw new ImageLoadException(ImageLoadError.IoError, "missing", path);
}

internal sealed class FakeClipboard : IClipboardService
{
    public string? Text { get; private set; }

    public string? Payload { get; private set; }

    public void SetText(string text) => Text = text;

    public void SetGlyph(MonoBitmap glyph, bool invert)
    {
        Payload = GlyphClipboard.ToPayload(glyph);
        Text = GlyphClipboard.ToText(glyph, invert);
    }

    public bool TryGetGlyph(bool invert, out MonoBitmap? glyph)
    {
        if (Payload is not null && GlyphClipboard.TryParsePayload(Payload, out glyph))
        {
            return true;
        }

        return GlyphClipboard.TryParseText(Text, invert, out glyph);
    }
}

internal sealed class StubOutlines : IGlyphOutlineProvider
{
    public IReadOnlyList<string> GetInstalledFamilies() => new[] { "Stub" };

    public bool TryGetMetrics(FontFaceSpec face, out FontMetrics metrics)
    {
        metrics = new FontMetrics(6, 2);
        return string.Equals(face.Family, "Stub", StringComparison.OrdinalIgnoreCase);
    }

    public GlyphOutline? GetOutline(FontFaceSpec face, char character)
    {
        if (!string.Equals(face.Family, "Stub", StringComparison.OrdinalIgnoreCase) || character != 'A')
        {
            return null;
        }

        OutlinePoint[] rectangle =
        {
            new(-2, -20),
            new(20, -20),
            new(20, 20),
            new(-2, 20),
        };
        return new GlyphOutline(new[] { rectangle });
    }
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
