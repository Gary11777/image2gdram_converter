using System.Text;
using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Settings;

namespace Image2Gdram.Core.Tests.Settings;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "i2g_settings_" + Guid.NewGuid().ToString("N"));

    public SettingsServiceTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Missing_file_loads_defaults_without_a_diagnostic()
    {
        SettingsLoadResult result = Service().Load();

        Assert.Null(result.Diagnostic);
        Assert.Equal(AppSettings.DefaultLanguage, result.Settings.Language);
        Assert.Null(result.Settings.Image.SourcePath);
        Assert.Empty(result.Settings.UserPresets.Presets);
        Assert.Empty(result.Settings.RecentFiles);
        Assert.Equal(FontTabParameters.DefaultPreviewText, result.Settings.Font.Parameters.PreviewText);
        Assert.Equal(ColorScheme.Oled, result.Settings.Image.Parameters.ColorScheme);
        Assert.Equal("image", result.Settings.Image.Parameters.Output.ArrayName);
        Assert.Equal("font", result.Settings.Font.Parameters.Output.ArrayName);
        Assert.Equal(PackingOptions.Default, result.Settings.Image.Parameters.Packing);
    }

    [Fact]
    public void Save_then_load_matches_and_the_second_save_is_byte_identical()
    {
        SettingsService service = Service();
        AppSettings settings = Sample();
        service.Save(settings);
        byte[] first = File.ReadAllBytes(service.FilePath);

        SettingsLoadResult loaded = service.Load();
        Assert.Null(loaded.Diagnostic);
        AssertSame(settings, loaded.Settings);

        service.Save(loaded.Settings);
        Assert.Equal(first, File.ReadAllBytes(service.FilePath));

        Assert.False(first.AsSpan(0, 3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }));
        string text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(first);
        Assert.Contains("\u041f\u0440\u0438\u0432\u0435\u0442", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u041F", text, StringComparison.Ordinal);
        Assert.Contains("\"formatVersion\": 1", text, StringComparison.Ordinal);
        Assert.Contains("\"direction\": \"Horizontal\"", text, StringComparison.Ordinal);
        Assert.Contains("\"colorScheme\": \"Lcd\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("generatedAt", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Directory.EnumerateFiles(Path.GetDirectoryName(service.FilePath)!), path => path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Saving_does_not_change_or_open_the_source_file()
    {
        string source = Path.Combine(_root, "pic.bin");
        byte[] bytes = { 1, 2, 3, 4 };
        File.WriteAllBytes(source, bytes);
        var settings = AppSettings.CreateDefault();
        settings.Image = settings.Image with { SourcePath = source };

        SettingsService service = Service();
        service.Save(settings);

        Assert.Equal(bytes, File.ReadAllBytes(source));
        Assert.Equal(source, service.Load().Settings.Image.SourcePath);
    }

    [Fact]
    public void Corrupt_unknown_version_and_numeric_enum_reset_without_throwing()
    {
        SettingsService service = Service();
        service.Save(AppSettings.CreateDefault());
        string original = File.ReadAllText(service.FilePath);

        File.WriteAllText(service.FilePath, "{");
        SettingsLoadResult broken = service.Load();
        Assert.Equal(DiagnosticCode.SettingsFileReset, broken.Diagnostic!.Code);
        Assert.Equal(DiagnosticSeverity.Warning, broken.Diagnostic.Severity);
        Assert.Equal(service.FilePath, broken.Diagnostic.Arguments[0]);
        Assert.Equal(AppSettings.DefaultLanguage, broken.Settings.Language);
        Assert.Equal("{", File.ReadAllText(service.FilePath));

        File.WriteAllText(service.FilePath, original.Replace("\"formatVersion\": 1", "\"formatVersion\": 2", StringComparison.Ordinal));
        Assert.NotNull(service.Load().Diagnostic);

        File.WriteAllText(service.FilePath, original.Replace("\"Oled\"", "0", StringComparison.Ordinal));
        Assert.NotNull(service.Load().Diagnostic);
    }

    [Fact]
    public void User_preset_that_copies_a_built_in_name_resets_the_file()
    {
        SettingsService service = Service();
        service.Save(Sample());
        string text = File.ReadAllText(service.FilePath).Replace("My panel", "WG240128A", StringComparison.Ordinal);
        File.WriteAllText(service.FilePath, text);

        SettingsLoadResult result = service.Load();

        Assert.Equal(DiagnosticCode.SettingsFileReset, result.Diagnostic!.Code);
        Assert.Empty(result.Settings.UserPresets.Presets);
    }

    [Fact]
    public void Recent_files_keep_the_newest_ten_without_duplicates()
    {
        string dir = Path.Combine(_root, "files");
        Directory.CreateDirectory(dir);
        var settings = AppSettings.CreateDefault();
        for (int i = 0; i < 12; i++)
        {
            settings.RememberRecent(Path.Combine(dir, $"f{i}.png"));
        }

        settings.RememberRecent(Path.Combine(dir, "F11.png"));

        Assert.Equal(10, settings.RecentFiles.Count);
        Assert.Equal(Path.GetFullPath(Path.Combine(dir, "F11.png")), settings.RecentFiles[0]);
        Assert.Equal(Path.GetFullPath(Path.Combine(dir, "f2.png")), settings.RecentFiles[9]);
        Assert.Single(settings.RecentFiles, path => path.EndsWith("f11.png", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(settings.RecentFiles, path => path.EndsWith("f0.png", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(settings.RecentFiles, path => path.EndsWith("f1.png", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Writable_directory_keeps_settings_beside_the_program()
    {
        string exe = Path.Combine(_root, "exe");
        string path = SettingsPathResolver.Resolve(exe, Path.Combine(_root, "appdata"));

        Assert.Equal(Path.Combine(Path.GetFullPath(exe), "settings.json"), path);
        Assert.Empty(Directory.GetFiles(exe));
    }

    [Fact]
    public void Unwritable_base_directory_uses_appdata_imageiu()
    {
        string blocked = Path.Combine(_root, "not-a-directory");
        File.WriteAllText(blocked, "x");
        string appData = Path.Combine(_root, "appdata");

        string path = SettingsPathResolver.Resolve(blocked, appData);

        Assert.Equal(Path.Combine(appData, "ImageIU", "settings.json"), path);
        Assert.True(Directory.Exists(Path.Combine(appData, "ImageIU")));
    }

    [Fact]
    public void Both_locations_unwritable_is_an_access_error()
    {
        string blocked = Path.Combine(_root, "not-a-directory");
        File.WriteAllText(blocked, "x");
        string appData = Path.Combine(_root, "appdata-file");
        File.WriteAllText(appData, "x");

        SettingsException exception = Assert.Throws<SettingsException>(() => SettingsPathResolver.Resolve(blocked, appData));

        Assert.Equal(SettingsError.AccessDenied, exception.Error);
    }

    private SettingsService Service() => new(Path.Combine(_root, "exe"), Path.Combine(_root, "appdata"));

    private AppSettings Sample()
    {
        string dir = Path.Combine(_root, "files");
        Directory.CreateDirectory(dir);
        var store = new UserPresetStore(PresetCatalog.Shared);
        store.Add(new Preset(
            "My panel",
            "lab",
            20,
            10,
            PackingOptions.Default with
            {
                Direction = PackDirection.Horizontal,
                BitOrder = BitOrder.MsbFirst,
                BitsPerByte = 6,
            },
            ColorScheme.Lcd,
            isBuiltIn: false));
        var settings = new AppSettings(store)
        {
            Language = "de-DE",
            Image = new ImageSettingsTab
            {
                SourcePath = Path.Combine(dir, "logo.png"),
                Parameters = ImageTabParameters.CreateDefault() with
                {
                    SelectedFrame = 2,
                    ExportAllFrames = true,
                    ColorScheme = ColorScheme.Lcd,
                    Preset = CustomBased("WG240128A"),
                    Packing = PackingOptions.Default with { Invert = true },
                },
            },
            Font = new FontSettingsTab
            {
                SheetPath = Path.Combine(dir, "sheet.png"),
                ImportPath = Path.Combine(dir, "font.c"),
                Parameters = FontTabParameters.CreateDefault() with
                {
                    PreviewText = FontTabParameters.DefaultPreviewText,
                    CellWidth = 12,
                    CellHeight = 16,
                    CustomCodes = new[] { 0x10, 0x98 },
                    RangePresets = CharRangePreset.Latin | CharRangePreset.OtherCp1251,
                },
            },
            Folders = new LastFolders
            {
                OpenImage = Path.Combine(dir, "in"),
                OpenFont = Path.Combine(dir, "fonts"),
                SaveOutput = Path.Combine(dir, "out"),
                OpenProject = Path.Combine(dir, "projects"),
                SaveProject = Path.Combine(dir, "projects"),
            },
        };
        settings.RememberRecent(Path.Combine(dir, "a.png"));
        settings.RememberRecent(Path.Combine(dir, "b.png"));
        return settings;
    }

    private static void AssertSame(AppSettings expected, AppSettings actual)
    {
        Assert.Equal(expected.Language, actual.Language);
        Assert.Equal(expected.Image.SourcePath, actual.Image.SourcePath);
        Assert.Equal(expected.Image.Parameters.SelectedFrame, actual.Image.Parameters.SelectedFrame);
        Assert.Equal(expected.Image.Parameters.ExportAllFrames, actual.Image.Parameters.ExportAllFrames);
        Assert.Equal(expected.Image.Parameters.ColorScheme, actual.Image.Parameters.ColorScheme);
        Assert.Equal(expected.Image.Parameters.Preset, actual.Image.Parameters.Preset);
        Assert.Equal(expected.Image.Parameters.Packing, actual.Image.Parameters.Packing);
        Assert.Equal(expected.Image.Parameters.Processing, actual.Image.Parameters.Processing);
        Assert.Equal(expected.Image.Parameters.Output with { GeneratedAt = null }, actual.Image.Parameters.Output);
        Assert.Equal(expected.Font.SheetPath, actual.Font.SheetPath);
        Assert.Equal(expected.Font.ImportPath, actual.Font.ImportPath);
        Assert.Equal(expected.Font.Parameters.PreviewText, actual.Font.Parameters.PreviewText);
        Assert.Equal(expected.Font.Parameters.CellWidth, actual.Font.Parameters.CellWidth);
        Assert.Equal(expected.Font.Parameters.CellHeight, actual.Font.Parameters.CellHeight);
        Assert.Equal(expected.Font.Parameters.RangePresets, actual.Font.Parameters.RangePresets);
        Assert.Equal(expected.Font.Parameters.CustomCodes, actual.Font.Parameters.CustomCodes);
        Assert.Equal(expected.Folders, actual.Folders);
        Assert.Equal(expected.RecentFiles, actual.RecentFiles);
        Assert.Equal(expected.UserPresets.Presets.Select(Describe).ToArray(), actual.UserPresets.Presets.Select(Describe).ToArray());
    }

    private static string Describe(Preset preset) =>
        $"{preset.Name}|{preset.Controller}|{preset.Width}x{preset.Height}|{preset.ColorScheme}|{preset.IsBuiltIn}|{preset.Packing}";

    private static PresetBinding CustomBased(string name) => new() { BasedOn = name };
}
