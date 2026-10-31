using System.Xml.Linq;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Projects;
using Image2Gdram.Core.Settings;
using image2gdram_converter.Controls;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.App.Tests;

/// <summary>Поле «Схема» снова последнее в разделе «Упаковка» (N-81, поведение до N-75).</summary>
public class SchemeFieldTests : IDisposable
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-scheme-" + Guid.NewGuid().ToString("N"));

    public SchemeFieldTests() => Directory.CreateDirectory(_directory);

    [Theory]
    [InlineData("ImageConverterView.xaml")]
    [InlineData("FontGeneratorView.xaml")]
    public void Packing_section_ends_with_the_scheme_field(string file)
    {
        XDocument view = Load(file);
        XElement packing = Marked(view, "Section.Packing");
        XElement output = Marked(view, "Section.Output");

        Assert.Equal(
            new[]
            {
                "{DynamicResource Label.Direction}",
                "{DynamicResource Label.BitOrder}",
                "{DynamicResource Label.Bits}",
                "{DynamicResource Label.Traversal}",
                "{DynamicResource Label.Scheme}",
            },
            LabelsBetween(view, packing, output));

        XElement scheme = view.Descendants(Presentation + "ComboBox")
            .Single(element => element.Attribute("ItemsSource")?.Value == "{Binding Schemes}");
        Assert.True(IsBetween(scheme, packing, output));
        Assert.True(IsBefore(Marked(view, "Label.Traversal"), scheme));

        XElement invert = view.Descendants(Presentation + "CheckBox")
            .Single(element => element.Attribute("Content")?.Value == "{DynamicResource Check.Invert}");
        Assert.True(IsBetween(invert, packing, output));
        Assert.Equal("0,4,0,0", invert.Attribute("Margin")?.Value);
        Assert.True(IsBefore(invert, scheme));
    }

    [Fact]
    public void Dictionary_contains_the_scheme_keys()
    {
        Dictionary<string, string> keys = TestFiles.LoadUiStrings();
        Assert.Equal("Схема", keys["Label.Scheme"]);
        Assert.Equal("ЖКИ", keys["Opt.Scheme.Lcd"]);
        Assert.Equal("OLED", keys["Opt.Scheme.Oled"]);
    }

    [Fact]
    public void Image_presets_set_the_scheme_and_a_manual_change_is_saved()
    {
        ImageConverterViewModel vm = CreateImage();
        Assert.Equal(ColorScheme.Oled, vm.Scheme);

        Select(vm, "OLED128X64-0.96");
        Assert.Equal(ColorScheme.Oled, vm.Scheme);
        Assert.Equal("OLED128X64-0.96", vm.PresetCaption);

        Select(vm, "WG240128A");
        Assert.Equal(ColorScheme.Lcd, vm.Scheme);
        Assert.Equal("WG240128A", vm.PresetCaption);

        vm.Scheme = ColorScheme.Oled;
        Assert.Equal("Пользовательский (на основе WG240128A)", vm.PresetCaption);

        ImageTabParameters saved = vm.ToParameters();
        Assert.Equal(ColorScheme.Oled, saved.ColorScheme);
        vm.Scheme = ColorScheme.Lcd;
        vm.LoadState(saved, schedule: false);
        Assert.Equal(ColorScheme.Oled, vm.Scheme);
        Assert.Equal(ColorScheme.Oled, vm.ToParameters().ColorScheme);

        var dialogs = new FakeDialogs { TextAnswer = "CustomImage" };
        ImageConverterViewModel saving = CreateImage(dialogs);
        saving.Scheme = ColorScheme.Lcd;
        saving.SavePresetCommand.Execute(null);
        Preset stored = saving.Presets.Items.Select(item => item.Preset).Single(preset => preset != null && (preset.Name == "CustomImage" || preset.Name == "CustomFont"))!;
        Assert.Equal(ColorScheme.Lcd, stored.ColorScheme);
    }

    [Fact]
    public void Font_presets_set_the_scheme_and_a_manual_change_is_saved()
    {
        FontGeneratorViewModel vm = CreateFont();
        Assert.Equal(ColorScheme.Oled, vm.Scheme);

        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == "OLED128X64-0.96");
        Assert.Equal(ColorScheme.Oled, vm.Scheme);
        Assert.Equal("OLED128X64-0.96", vm.PresetCaption);

        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == "WG240128A");
        Assert.Equal(ColorScheme.Lcd, vm.Scheme);

        vm.Scheme = ColorScheme.Oled;
        Assert.Equal("Пользовательский (на основе WG240128A)", vm.PresetCaption);

        FontTabParameters saved = vm.ToParameters();
        Assert.Equal(ColorScheme.Oled, saved.ColorScheme);
        vm.Load(new FontProjectTab(saved with { ColorScheme = ColorScheme.Lcd }, new FontTable(FontCellSize.Cell6x8)));
        Assert.Equal(ColorScheme.Lcd, vm.Scheme);
        vm.Load(new FontProjectTab(saved, new FontTable(FontCellSize.Cell6x8)));
        Assert.Equal(ColorScheme.Oled, vm.Scheme);

        var dialogs = new FakeDialogs { TextAnswer = "CustomFont" };
        FontGeneratorViewModel saving = CreateFont(dialogs);
        saving.Scheme = ColorScheme.Lcd;
        saving.SavePresetCommand.Execute(null);
        Preset stored = saving.Presets.Items.Select(item => item.Preset).Single(preset => preset != null && (preset.Name == "CustomImage" || preset.Name == "CustomFont"))!;
        Assert.Equal(ColorScheme.Lcd, stored.ColorScheme);
    }

    [Fact]
    public void Changing_the_scheme_does_not_change_image_or_font_code()
    {
        ImageConverterViewModel image = CreateImage();
        image.IncludeDate = false;
        image.LoadDecoded(
            new DecodedImage(ImageFileFormat.Png, new[] { new RgbaImage(4, 4, 0, 0, 0, 255) }),
            "dot.png",
            resetFrame: true,
            userAction: false);
        string imageCode = image.CodeText;
        var packing = image.ToParameters().Packing;
        image.Scheme = image.Scheme == ColorScheme.Oled ? ColorScheme.Lcd : ColorScheme.Oled;
        Assert.Equal(imageCode, image.CodeText);
        Assert.Equal(packing, image.ToParameters().Packing);

        FontGeneratorViewModel font = CreateFont();
        font.IncludeDate = false;
        string fontCode = font.CodeText;
        var fontPacking = font.ToParameters().Packing;
        font.Scheme = font.Scheme == ColorScheme.Oled ? ColorScheme.Lcd : ColorScheme.Oled;
        Assert.Equal(fontCode, font.CodeText);
        Assert.Equal(fontPacking, font.ToParameters().Packing);
    }

    [Fact]
    public void Preview_controls_default_scheme_metadata_to_oled()
    {
        Assert.Equal(ColorScheme.Oled, PixelGridControl.SchemeProperty.DefaultMetadata.DefaultValue);
        Assert.Equal(ColorScheme.Oled, GlyphTableControl.SchemeProperty.DefaultMetadata.DefaultValue);
        Assert.Equal(ColorScheme.Oled, StringPreviewControl.SchemeProperty.DefaultMetadata.DefaultValue);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private ImageConverterViewModel CreateImage(FakeDialogs? dialogs = null)
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        return new ImageConverterViewModel(
            new MapText(TestFiles.LoadUiStrings()),
            dialogs ?? new FakeDialogs(),
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            PresetCatalog.Shared,
            settings.UserPresets,
            settings.Image.Parameters);
    }

    private FontGeneratorViewModel CreateFont(FakeDialogs? dialogs = null)
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        return new FontGeneratorViewModel(
            new MapText(TestFiles.LoadUiStrings()),
            dialogs ?? new FakeDialogs(),
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            new StubOutlines(),
            PresetCatalog.Shared,
            settings.UserPresets,
            settings.Font);
    }

    private static void Select(ImageConverterViewModel vm, string name) =>
        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == name);

    private static XDocument Load(string file)
    {
        string path = Path.Combine(TestFiles.RepoRoot(), "src", "image2gdram_converter", "Views", file);
        return XDocument.Load(path);
    }

    private static XElement Marked(XContainer root, string key) =>
        root.Descendants(Presentation + "TextBlock").Single(element =>
            element.Attribute("Text")?.Value == "{DynamicResource " + key + "}");

    private static string[] LabelsBetween(XContainer root, XNode start, XNode end) =>
        root.Descendants(Presentation + "TextBlock")
            .Where(element => IsBetween(element, start, end))
            .Select(element => element.Attribute("Text")?.Value)
            .Where(value => value is not null && value.StartsWith("{DynamicResource Label.", StringComparison.Ordinal))
            .Cast<string>()
            .ToArray();

    private static bool IsBefore(XNode earlier, XNode later) =>
        XNode.CompareDocumentOrder(earlier, later) < 0;

    private static bool IsBetween(XNode node, XNode start, XNode end) =>
        IsBefore(start, node) && IsBefore(node, end);
}
