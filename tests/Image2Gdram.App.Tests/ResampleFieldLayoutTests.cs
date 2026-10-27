using System.Xml.Linq;

namespace Image2Gdram.App.Tests;

/// <summary>The resample algorithm is labeled and placed after the threshold (N-74).</summary>
public class ResampleFieldLayoutTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [Fact]
    public void Resample_field_follows_the_threshold_inside_binarize()
    {
        XDocument view = Load("ImageConverterView.xaml");
        XElement size = Marked(view, "Section.Size");
        XElement binarize = Marked(view, "Section.Binarize");
        XElement packing = Marked(view, "Section.Packing");
        XElement resample = Marked(view, "Label.Resample");
        XElement threshold = Marked(view, "Label.Threshold");

        Assert.True(IsBefore(binarize, resample));
        Assert.True(IsBefore(threshold, resample));
        Assert.True(IsBefore(resample, packing));
        Assert.False(IsBetween(resample, size, binarize));

        string[] sizeLabels = LabelsBetween(view, size, binarize);
        Assert.Equal("{DynamicResource Label.TransformScale}", sizeLabels[^1]);
        Assert.DoesNotContain("{DynamicResource Label.Resample}", sizeLabels);

        Assert.Equal(
            new[]
            {
                "{DynamicResource Label.Binarize}",
                "{DynamicResource Label.Threshold}",
                "{DynamicResource Label.Resample}",
            },
            LabelsBetween(view, binarize, packing));

        XElement grid = Assert.IsType<XElement>(resample.Parent);
        Assert.Equal(Presentation + "Grid", grid.Name);
        Assert.Equal("0,2", grid.Attribute("Margin")?.Value);
        XElement column = grid.Element(Presentation + "Grid.ColumnDefinitions")!
            .Elements(Presentation + "ColumnDefinition")
            .First();
        Assert.Equal("140", column.Attribute("Width")?.Value);

        XElement combo = grid.Elements(Presentation + "ComboBox").Single();
        Assert.Equal("{Binding Resamples}", combo.Attribute("ItemsSource")?.Value);
        Assert.Equal("{Binding Resample}", combo.Attribute("SelectedValue")?.Value);
    }

    [Fact]
    public void Resample_caption_is_the_agreed_word()
    {
        Dictionary<string, string> keys = TestFiles.LoadUiStrings();
        Assert.Equal("\u0413\u0440\u0430\u043d\u044c", keys["Label.Resample"]);
        Assert.Equal("\u041c\u0430\u0441\u0448\u0442\u0430\u0431", keys["Label.Zoom"]);
        Assert.Equal("\u041c\u0430\u0441\u0448\u0442\u0430\u0431 \u0441\u0442\u0440\u043e\u043a\u0438", keys["Label.PreviewScale"]);
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
