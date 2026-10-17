using System.Xml.Linq;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Projects;
using Image2Gdram.Core.Settings;
using image2gdram_converter.Controls;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.App.Tests;

/// <summary>The scheme field is removed from the packing section and display always uses the LCD scheme (N-75).</summary>
public class SchemeFieldRemovalTests : IDisposable
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-scheme-" + Guid.NewGuid().ToString("N"));

    public SchemeFieldRemovalTests() => Directory.CreateDirectory(_directory);

    [Theory]
    [InlineData("ImageConverterView.xaml")]
    [InlineData("FontGeneratorView.xaml")]
    public void Packing_section_contains_only_packing_fields_and_invert_without_scheme(string file)
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
            },
            LabelsBetween(view, packing, output));

        Assert.DoesNotContain(
            view.Descendants(Presentation + "TextBlock"),
            element => element.Attribute("Text")?.Value == "{DynamicResource Label.Scheme}");
        Assert.DoesNotContain(
            view.Descendants(Presentation + "ComboBox"),
            element => element.Attribute("ItemsSource")?.Value == "{Binding Schemes}");

        XElement invert = view.Descendants(Presentation + "CheckBox")
            .Single(element => element.Attribute("Content")?.Value == "{DynamicResource Check.Invert}");
        Assert.True(IsBetween(invert, packing, output));
        Assert.Equal("0,4,0,0", invert.Attribute("Margin")?.Value);
    }

    [Fact]
    public void Dictionary_does_not_contain_scheme_keys()
    {
        Dictionary<string, string> keys = TestFiles.LoadUiStrings();
        Assert.False(keys.ContainsKey("Label.Scheme"));
        Assert.False(keys.ContainsKey("Opt.Scheme.Lcd"));
        Assert.False(keys.ContainsKey("Opt.Scheme.Oled"));
    }

    [Fact]
    public void Image_view_model_always_uses_lcd_scheme_for_oled_preset_loaded_parameters_and_saved_preset()
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        var dialogs = new FakeDialogs();
        var vm = new ImageConverterViewModel(
            new MapText(TestFiles.LoadUiStrings()),
            dialogs,
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            PresetCatalog.Shared,
            settings.UserPresets,
            ImageTabParameters.CreateDefault() with { ColorScheme = ColorScheme.Oled });

        Assert.Equal(ColorScheme.Lcd, vm.Scheme);
        Assert.Equal(ColorScheme.Lcd, vm.ToParameters().ColorScheme);

        Preset oled = PresetCatalog.Shared.BuiltIn.First(item => item.Name == "OLED128X64-0.96");
        Assert.Equal(ColorScheme.Oled, oled.ColorScheme);
        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == oled.Name);

        Assert.Equal(oled.Width, vm.ToParameters().Processing.TargetWidth);
        Assert.Equal(oled.Height, vm.ToParameters().Processing.TargetHeight);
        Assert.Equal(oled.Packing, vm.ToParameters().Packing);
        Assert.Equal(ColorScheme.Lcd, vm.Scheme);
        Assert.Equal(ColorScheme.Lcd, vm.ToParameters().ColorScheme);
        Assert.Equal("OLED128X64-0.96", vm.PresetCaption);
        Assert.Null(vm.ToParameters().Preset.BasedOn);

        vm.LoadState(ImageTabParameters.CreateDefault() with { ColorScheme = ColorScheme.Oled }, schedule: false);
        Assert.Equal(ColorScheme.Lcd, vm.Scheme);
        Assert.Equal(ColorScheme.Lcd, vm.ToParameters().ColorScheme);

        dialogs.TextAnswer = "CustomImage";
        vm.SavePresetCommand.Execute(null);
        Preset saved = Assert.Single(settings.UserPresets.Presets);
        Assert.Equal(ColorScheme.Lcd, saved.ColorScheme);
    }

    [Fact]
    public void Font_view_model_always_uses_lcd_scheme_for_oled_preset_loaded_parameters_and_saved_preset()
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        var dialogs = new FakeDialogs();
        var fontSettings = new FontSettingsTab
        {
            Parameters = settings.Font.Parameters with { ColorScheme = ColorScheme.Oled },
        };
        var vm = new FontGeneratorViewModel(
            new MapText(TestFiles.LoadUiStrings()),
            dialogs,
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            new StubOutlines(),
            PresetCatalog.Shared,
            settings.UserPresets,
            fontSettings);

        Assert.Equal(ColorScheme.Lcd, vm.Scheme);
        Assert.Equal(ColorScheme.Lcd, vm.ToParameters().ColorScheme);

        Preset oled = PresetCatalog.Shared.BuiltIn.First(item => item.Name == "OLED128X64-0.96");
        Assert.Equal(ColorScheme.Oled, oled.ColorScheme);
        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == oled.Name);

        Assert.Equal(oled.Packing, vm.ToParameters().Packing);
        Assert.Equal(ColorScheme.Lcd, vm.Scheme);
        Assert.Equal(ColorScheme.Lcd, vm.ToParameters().ColorScheme);
        Assert.Equal("OLED128X64-0.96", vm.PresetCaption);
        Assert.Null(vm.ToParameters().Preset.BasedOn);

        FontTabParameters oledParameters = FontTabParameters.CreateDefault() with { ColorScheme = ColorScheme.Oled };
        vm.Load(new FontProjectTab(oledParameters, new FontTable(FontCellSize.Cell6x8)));
        Assert.Equal(ColorScheme.Lcd, vm.Scheme);
        Assert.Equal(ColorScheme.Lcd, vm.ToParameters().ColorScheme);

        dialogs.TextAnswer = "CustomFont";
        vm.SavePresetCommand.Execute(null);
        Preset saved = Assert.Single(settings.UserPresets.Presets);
        Assert.Equal(ColorScheme.Lcd, saved.ColorScheme);
    }

    [Fact]
    public void Preview_controls_default_scheme_metadata_to_lcd()
    {
        Assert.Equal(ColorScheme.Lcd, PixelGridControl.SchemeProperty.DefaultMetadata.DefaultValue);
        Assert.Equal(ColorScheme.Lcd, GlyphTableControl.SchemeProperty.DefaultMetadata.DefaultValue);
        Assert.Equal(ColorScheme.Lcd, StringPreviewControl.SchemeProperty.DefaultMetadata.DefaultValue);
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
