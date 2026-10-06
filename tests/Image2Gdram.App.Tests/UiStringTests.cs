using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Image2Gdram.App.Tests;

public class UiStringTests
{
    [Fact]
    public void Dictionary_keys_used_by_the_shell_exist()
    {
        string root = TestFiles.RepoRoot();
        string app = Path.Combine(root, "src", "image2gdram_converter");
        var keys = TestFiles.LoadUiStrings();
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.GetFiles(app, "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            if (file.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                && !file.EndsWith("Strings.ru-RU.xaml", StringComparison.OrdinalIgnoreCase))
            {
                foreach (Match match in Regex.Matches(text, @"DynamicResource\s+([A-Za-z0-9_.]+)"))
                {
                    used.Add(match.Groups[1].Value);
                }
            }

            if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                foreach (Match match in Regex.Matches(text, """(?:Get|Format)\("([^"]+)"\)"""))
                {
                    used.Add(match.Groups[1].Value);
                }
            }
        }

        var missing = used.Where(key => !keys.ContainsKey(key)).OrderBy(key => key).ToArray();
        Assert.True(missing.Length == 0, "Missing dictionary keys: " + string.Join(", ", missing));
    }

    [Fact]
    public void Interface_literals_are_not_written_in_code_or_markup()
    {
        string app = Path.Combine(TestFiles.RepoRoot(), "src", "image2gdram_converter");
        var offenders = new List<string>();
        foreach (string file in Directory.GetFiles(app, "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.EndsWith("Strings.ru-RU.xaml", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            if (file.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) && HasCyrillic(text))
            {
                offenders.Add(Path.GetFileName(file));
                continue;
            }

            if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && QuotedCyrillic(text))
            {
                offenders.Add(Path.GetFileName(file));
            }
        }

        Assert.True(offenders.Count == 0, "Cyrillic UI text outside the dictionary: " + string.Join(", ", offenders));
    }

    [Fact]
    public void Custom_preset_caption_matches_the_agreed_phrase()
    {
        var keys = TestFiles.LoadUiStrings();
        Assert.Equal("Пользовательский (на основе {0})", keys["Preset.CustomBasedOn"]);
    }

    private static bool HasCyrillic(string text) => text.Any(ch => ch is >= '\u0400' and <= '\u04FF');

    private static bool QuotedCyrillic(string text)
    {
        bool quote = false;
        bool escape = false;
        var current = new System.Text.StringBuilder();
        foreach (char ch in StripComments(text))
        {
            if (!quote)
            {
                if (ch == '"')
                {
                    quote = true;
                    current.Clear();
                }

                continue;
            }

            if (escape)
            {
                current.Append(ch);
                escape = false;
                continue;
            }

            if (ch == '\\')
            {
                escape = true;
                continue;
            }

            if (ch == '"')
            {
                if (HasCyrillic(current.ToString()))
                {
                    return true;
                }

                quote = false;
                continue;
            }

            current.Append(ch);
        }

        return false;
    }

    private static string StripComments(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length);
        bool line = false;
        bool block = false;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            char next = i + 1 < text.Length ? text[i + 1] : '\0';
            if (line)
            {
                if (ch == '\n')
                {
                    line = false;
                    builder.Append(ch);
                }

                continue;
            }

            if (block)
            {
                if (ch == '*' && next == '/')
                {
                    block = false;
                    i++;
                }

                continue;
            }

            if (ch == '/' && next == '/')
            {
                line = true;
                i++;
                continue;
            }

            if (ch == '/' && next == '*')
            {
                block = true;
                i++;
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }
}
