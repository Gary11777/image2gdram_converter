using System.Xml.Linq;

namespace Image2Gdram.App.Tests;

/// <summary>Zoom and the grid checkbox sit in the result header (N-78).</summary>
public class ZoomFieldLayoutTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Zoom_and_grid_sit_in_the_result_header()
    {
        XDocument view = Load();
        XElement zoom = Marked(view, "Label.Zoom");
        XElement combo = ZoomCombo(view);
        XElement gridCheck = GridCheck(view);
        XElement group = zoom.Parent!;

        Assert.Same(group, combo.Parent);
        Assert.Same(group, gridCheck.Parent);
        Assert.True(IsBefore(zoom, combo));
        Assert.True(IsBefore(combo, gridCheck));
        Assert.DoesNotContain(
            group.Elements(),
            element => element.Attribute("Text")?.Value == "{DynamicResource Label.Result}");

        XElement pixels = Pixels(view);
        XElement layout = pixels.Ancestors(Presentation + "Grid").First(grid =>
            ColumnWidths(grid).SequenceEqual(new[] { "*", "2*" }));
        XElement header = layout.Elements().Single(element =>
            element.Attribute("Grid.Column")?.Value == "1" &&
            element.Attribute("Grid.Row")?.Value == "0");

        Assert.Contains(group, header.Descendants());
        Assert.Equal("1", pixels.Attribute("Grid.Column")?.Value);
        Assert.Same(layout, pixels.Parent);

        XElement top = TopBar(view);
        Assert.DoesNotContain(top.Descendants(), element => element == zoom || element == combo || element == gridCheck);
        foreach (XElement sourceCell in layout.Elements().Where(element => element.Attribute("Grid.Column")?.Value == "0"))
        {
            Assert.DoesNotContain(sourceCell.DescendantsAndSelf(), element => element == zoom || element == combo || element == gridCheck);
        }

        XElement result = Marked(view, "Label.Result");
        Assert.True(IsBefore(result, zoom));
        Assert.Contains(result, header.Descendants());
        Assert.True(ColumnOf(header, result) < ColumnOf(header, group));

        string[] widths = ColumnWidths(group.Parent!);
        int column = int.Parse(group.Attribute("Grid.Column")?.Value ?? "0");
        Assert.Equal(new[] { "*", "Auto", "*" }, widths);
        Assert.Equal("Auto", widths[column]);
        Assert.Equal("*", widths[column - 1]);
        Assert.Equal("*", widths[column + 1]);
        Assert.Equal("140", combo.Attribute("Width")?.Value);
    }

    [Fact]
    public void Top_bar_keeps_undo_redo_and_the_result_size()
    {
        XDocument view = Load();
        XElement top = TopBar(view);

        Assert.Contains(
            top.Descendants(Presentation + "Button"),
            element => element.Attribute("Command")?.Value == "{Binding UndoCommand}");
        Assert.Contains(
            top.Descendants(Presentation + "Button"),
            element => element.Attribute("Command")?.Value == "{Binding RedoCommand}");
        Assert.Contains(
            top.Descendants(Presentation + "TextBlock"),
            element => element.Attribute("Text")?.Value == "{Binding ResultSizeText}");
        Assert.DoesNotContain(
            top.Descendants(Presentation + "CheckBox"),
            element => element.Attribute("Content")?.Value == "{DynamicResource Check.Grid}");
        Assert.DoesNotContain(
            top.Descendants(Presentation + "ComboBox"),
            element => element.Attribute("ItemsSource")?.Value == "{Binding Zooms}");
    }

    [Fact]
    public void Zoom_combo_and_grid_check_appear_once()
    {
        XDocument view = Load();
        Assert.Single(view.Descendants(Presentation + "CheckBox"), element =>
            element.Attribute("Content")?.Value == "{DynamicResource Check.Grid}");
        Assert.Single(view.Descendants(Presentation + "ComboBox"), element =>
            element.Attribute("ItemsSource")?.Value == "{Binding Zooms}");
    }

    private static XDocument Load()
    {
        string path = Path.Combine(TestFiles.RepoRoot(), "src", "image2gdram_converter", "Views", "ImageConverterView.xaml");
        return XDocument.Load(path);
    }

    private static XElement Marked(XContainer root, string key) =>
        root.Descendants(Presentation + "TextBlock").Single(element =>
            element.Attribute("Text")?.Value == "{DynamicResource " + key + "}");

    private static XElement ZoomCombo(XContainer root) =>
        root.Descendants(Presentation + "ComboBox").Single(element =>
            element.Attribute("ItemsSource")?.Value == "{Binding Zooms}");

    private static XElement GridCheck(XContainer root) =>
        root.Descendants(Presentation + "CheckBox").Single(element =>
            element.Attribute("Content")?.Value == "{DynamicResource Check.Grid}");

    private static XElement Pixels(XContainer root) =>
        root.Descendants().Single(element =>
            element.Name.LocalName == "PixelGridControl" &&
            element.Attribute(Xaml + "Name")?.Value == "Pixels");

    private static XElement TopBar(XContainer root) =>
        root.Descendants(Presentation + "DockPanel").Single(element =>
            element.Attribute("Grid.Row")?.Value == "0");

    private static string[] ColumnWidths(XElement grid) =>
        grid.Element(Presentation + "Grid.ColumnDefinitions")?
            .Elements(Presentation + "ColumnDefinition")
            .Select(column => column.Attribute("Width")?.Value ?? "*")
            .ToArray()
        ?? Array.Empty<string>();

    private static int ColumnOf(XElement header, XElement element)
    {
        XElement placed = element;
        while (!ReferenceEquals(placed.Parent, header))
        {
            placed = placed.Parent ?? throw new InvalidOperationException("Element is outside the result header.");
        }

        return int.Parse(placed.Attribute("Grid.Column")?.Value ?? "0");
    }

    private static bool IsBefore(XNode earlier, XNode later) =>
        XNode.CompareDocumentOrder(earlier, later) < 0;
}
