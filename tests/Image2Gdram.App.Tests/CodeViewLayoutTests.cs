using System.Xml.Linq;

namespace Image2Gdram.App.Tests;

/// <summary>Copy and Save output sit in the file row above the code editor (N-66).</summary>
public class CodeViewLayoutTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [Fact]
    public void Copy_and_save_buttons_are_in_the_code_file_row()
    {
        XDocument code = Load("Controls", "CodeView.xaml");
        XElement tabs = code.Descendants(Presentation + "ListBox").Single(element =>
            Visibility(element).Contains("ShowCodeFiles", StringComparison.Ordinal));
        XElement copy = SingleButton(code, "CopyCommand");
        XElement save = SingleButton(code, "SaveOutputCommand");

        Assert.DoesNotContain(tabs.Descendants(), element => element == copy || element == save);
        Assert.Equal("Right", copy.Parent?.Attribute("DockPanel.Dock")?.Value);
        Assert.Same(copy.Parent, save.Parent);
        Assert.Equal(-1, XNode.CompareDocumentOrder(copy, save));

        XElement header = HeaderRow(copy);
        Assert.Equal("Top", header.Attribute("DockPanel.Dock")?.Value);
        Assert.DoesNotContain("ShowCodeFiles", Visibility(header), StringComparison.Ordinal);
        Assert.Contains(tabs, header.Descendants());
        Assert.Contains(save, header.Descendants());
    }

    [Theory]
    [InlineData("Views", "ImageConverterView.xaml")]
    [InlineData("Views", "FontGeneratorView.xaml")]
    public void Left_panel_does_not_host_copy_or_save(string folder, string file)
    {
        XDocument view = Load(folder, file);
        XElement layout = view.Root!.Element(Presentation + "Grid")!;
        XElement panel = layout.Elements(Presentation + "ScrollViewer")
            .Single(element => element.Attribute("Grid.Column")?.Value == "0");

        Assert.Empty(Buttons(panel, "CopyCommand"));
        Assert.Empty(Buttons(panel, "SaveOutputCommand"));
    }

    private static XDocument Load(string folder, string file)
    {
        string path = Path.Combine(TestFiles.RepoRoot(), "src", "image2gdram_converter", folder, file);
        return XDocument.Load(path);
    }

    private static XElement SingleButton(XContainer root, string command) =>
        Assert.Single(Buttons(root, command));

    private static IEnumerable<XElement> Buttons(XContainer root, string command) =>
        root.Descendants(Presentation + "Button").Where(element =>
            (element.Attribute("Command")?.Value ?? "").Contains("{Binding " + command + "}", StringComparison.Ordinal));

    private static XElement HeaderRow(XElement button) =>
        button.Ancestors(Presentation + "DockPanel").First(element =>
            element.Attribute("DockPanel.Dock")?.Value == "Top");

    private static string Visibility(XElement element) => element.Attribute("Visibility")?.Value ?? "";
}
