using System.Xml.Linq;

namespace Image2Gdram.App.Tests;

/// <summary>Раздел «Размер» — пресет, ширина, высота, масштаб и прямоугольник (N-80).</summary>
public class CropFieldLayoutTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Size_section_contains_only_the_crop_controls()
    {
        XDocument view = XDocument.Load(Path.Combine(TestFiles.RepoRoot(), "src", "image2gdram_converter", "Views", "ImageConverterView.xaml"));
        XElement size = Marked(view, "Section.Size");
        XElement binarize = Marked(view, "Section.Binarize");
        string[] labels = view.Descendants(Presentation + "TextBlock")
            .Where(element => IsBetween(element, size, binarize))
            .Select(element => element.Attribute("Text")?.Value)
            .Where(value => value is not null && value.StartsWith("{DynamicResource Label.", StringComparison.Ordinal))
            .Cast<string>()
            .ToArray();

        Assert.Equal(
            new[]
            {
                "{DynamicResource Label.Preset}",
                "{DynamicResource Label.Width}",
                "{DynamicResource Label.Height}",
                "{DynamicResource Label.TransformScale}",
            },
            labels);

        Assert.Contains(view.Descendants(Presentation + "Button"), button =>
            button.Attribute("Content")?.Value == "{DynamicResource Button.FitCrop}");
        Assert.Contains(view.Descendants(Presentation + "Slider"), slider => slider.Attribute(Xaml + "Name")?.Value == "ScaleSlider");
        Assert.Contains(view.Descendants(), element => element.Name.LocalName == "SourceCropControl");
        Assert.DoesNotContain(view.Descendants(Presentation + "TextBlock"), block =>
            block.Attribute("Text")?.Value is "{DynamicResource Label.SizeMode}" or "{DynamicResource Label.Fit}" or "{DynamicResource Label.Alignment}");
    }

    [Fact]
    public void Removed_keys_are_gone_and_offset_labels_stay_on_the_font_tab()
    {
        string dictionary = File.ReadAllText(Path.Combine(TestFiles.RepoRoot(), "src", "image2gdram_converter", "Resources", "Strings.ru-RU.xaml"));
        foreach (string key in new[]
        {
            "Label.SizeMode", "Label.Fit", "Label.Alignment",
            "Opt.Size.Manual", "Opt.Size.Match",
            "Opt.Fit.None", "Opt.Fit.Fit", "Opt.Fit.Stretch", "Opt.Fit.Fill",
            "Opt.Align.TopLeft", "Opt.Align.Center", "Opt.Align.BottomRight",
        })
        {
            Assert.DoesNotContain("x:Key=\"" + key + "\"", dictionary, StringComparison.Ordinal);
        }

        Assert.Contains("x:Key=\"Label.OffsetX\"", dictionary, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"Label.OffsetY\"", dictionary, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"Label.TransformScale\"", dictionary, StringComparison.Ordinal);
        Assert.Equal("Масштаб преобразования", TestFiles.LoadUiStrings()["Label.TransformScale"]);
        Assert.Equal("Вписать", TestFiles.LoadUiStrings()["Button.FitCrop"]);

        string image = File.ReadAllText(Path.Combine(TestFiles.RepoRoot(), "src", "image2gdram_converter", "Views", "ImageConverterView.xaml"));
        string font = File.ReadAllText(Path.Combine(TestFiles.RepoRoot(), "src", "image2gdram_converter", "Views", "FontGeneratorView.xaml"));
        Assert.DoesNotContain("Label.OffsetX", image, StringComparison.Ordinal);
        Assert.DoesNotContain("Label.OffsetY", image, StringComparison.Ordinal);
        Assert.Contains("Label.OffsetX", font, StringComparison.Ordinal);
        Assert.Contains("Label.OffsetY", font, StringComparison.Ordinal);
    }

    private static XElement Marked(XContainer root, string key) =>
        root.Descendants(Presentation + "TextBlock").Single(element =>
            element.Attribute("Text")?.Value == "{DynamicResource " + key + "}");

    private static bool IsBetween(XNode node, XNode start, XNode end) =>
        XNode.CompareDocumentOrder(start, node) < 0 && XNode.CompareDocumentOrder(node, end) < 0;
}
