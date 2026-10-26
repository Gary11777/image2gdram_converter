using System.Text.RegularExpressions;

namespace Image2Gdram.App.Tests;

/// <summary>The README title shows the current release, not the product version 1.0 (N-79).</summary>
public class ReadmeVersionTests
{
    [Fact]
    public void Title_matches_the_version_line_and_the_changelog()
    {
        string root = TestFiles.RepoRoot();
        string readme = File.ReadAllText(Path.Combine(root, "README.md"));
        string changelog = File.ReadAllText(Path.Combine(root, "docs", "changelog.md"));
        string[] lines = readme.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        Match version = Regex.Match(
            lines.First(line => line.StartsWith("\u0422\u0435\u043a\u0443\u0449\u0430\u044f \u0432\u0435\u0440\u0441\u0438\u044f: ", StringComparison.Ordinal)),
            @"^(\u0422\u0435\u043a\u0443\u0449\u0430\u044f \u0432\u0435\u0440\u0441\u0438\u044f: )(\S+) \(");
        Match heading = Regex.Match(changelog, @"^## \[([^\]]+)\]", RegexOptions.Multiline);

        Assert.True(version.Success);
        Assert.True(heading.Success);
        Assert.Equal(version.Groups[2].Value, heading.Groups[1].Value);
        Assert.Equal("# Image2GDRAM Converter " + version.Groups[2].Value, lines[0]);
    }
}
