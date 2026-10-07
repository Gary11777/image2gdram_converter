using System.Text.RegularExpressions;
using System.Xml.Linq;
using Image2Gdram.Core.Imaging;
using image2gdram_converter;
using image2gdram_converter.Services;

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

    [Fact]
    public void External_dictionary_replaces_phrases_and_bad_files_do_not_stop_the_start()
    {
        string root = Path.Combine(Path.GetTempPath(), "i2g-lang-" + Guid.NewGuid().ToString("N"));
        string languages = Path.Combine(root, "Languages");
        Directory.CreateDirectory(languages);
        File.WriteAllText(
            Path.Combine(languages, "en-US.xaml"),
            """
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                xmlns:sys="clr-namespace:System;assembly=System.Runtime">
                <sys:String x:Key="Tab.Image">Image converter</sys:String>
            </ResourceDictionary>
            """);
        File.WriteAllText(Path.Combine(languages, "de-DE.xaml"), "<ResourceDictionary");
        File.WriteAllText(Path.Combine(languages, "fr-FR.xaml"), "<Button xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"/>");
        try
        {
            using var sta = new StaDispatcher();
            sta.Dispatcher.Invoke(() =>
            {
                var service = new LocalizationService();
                service.Add(new[] { KeyValuePair.Create("Tab.Image", "Конвертер картинок") });

                Assert.Equal(LanguageLoadResult.BuiltIn, service.LoadExternal("ru-RU", root));
                Assert.Equal(LanguageLoadResult.BuiltIn, service.LoadExternal(null, root));
                Assert.Equal("Конвертер картинок", service.Get("Tab.Image"));
                Assert.Equal(LanguageLoadResult.Loaded, service.LoadExternal("en-US", root));
                Assert.Equal("Image converter", service.Get("Tab.Image"));
                Assert.Equal(LanguageLoadResult.Broken, service.LoadExternal("de-DE", root));
                Assert.Equal(LanguageLoadResult.Broken, service.LoadExternal("fr-FR", root));
                Assert.Equal(LanguageLoadResult.NotFound, service.LoadExternal("es-ES", root));
                Assert.Equal(LanguageLoadResult.InvalidName, service.LoadExternal(@"..\..\evil", root));
                Assert.Equal("Image converter", service.Get("Tab.Image"));
            });

            var text = new MapText(TestFiles.LoadUiStrings());
            Assert.Null(UserText.Language(text, LanguageLoadResult.Loaded, "en-US"));
            Assert.Contains("es-ES", UserText.Language(text, LanguageLoadResult.NotFound, "es-ES"), StringComparison.Ordinal);
            Assert.Contains("de-DE", UserText.Language(text, LanguageLoadResult.Broken, "de-DE"), StringComparison.Ordinal);
            Assert.Contains("evil", UserText.Language(text, LanguageLoadResult.InvalidName, @"..\..\evil"), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Image_read_error_names_the_reason_once()
    {
        var text = new MapText(TestFiles.LoadUiStrings());
        var error = new ImageLoadException(ImageLoadError.IoError, "Cannot read the file.", "x.png", new IOException("Отказано в доступе."));

        string message = UserText.ImageLoad(text, error);

        Assert.Equal("Не удалось прочитать файл: Отказано в доступе.", message);
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
