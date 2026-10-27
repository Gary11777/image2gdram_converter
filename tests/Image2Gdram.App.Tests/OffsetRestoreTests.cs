using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Projects;
using Image2Gdram.Core.Settings;
using image2gdram_converter;
using image2gdram_converter.Controls;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.App.Tests;

/// <summary>N-65: пустое и недопустимое смещение при выходе из поля возвращается к последнему допустимому.</summary>
public class OffsetRestoreTests : IDisposable
{
    private static readonly Dictionary<string, string> Text = TestFiles.LoadUiStrings();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-offset-" + Guid.NewGuid().ToString("N"));

    public OffsetRestoreTests() => Directory.CreateDirectory(_directory);

    public static TheoryData<bool, string> FontBadValues => new()
    {
        { true, "" },
        { true, "33" },
        { true, "-33" },
        { true, "x" },
        { false, "" },
        { false, "33" },
        { false, "-33" },
        { false, "x" },
    };

    [Theory]
    [MemberData(nameof(FontBadValues))]
    public void Font_offset_returns_to_the_last_valid_value(bool horizontal, string bad)
    {
        FontGeneratorViewModel vm = CreateFont();
        vm.SourceKind = FontSourceKind.TrueType;
        Assert.True(vm.CanCopy);
        string name = horizontal ? nameof(vm.OffsetXText) : nameof(vm.OffsetYText);

        SetFontOffset(vm, horizontal, "4");
        SetFontOffset(vm, horizontal, bad);

        Assert.NotEmpty(Errors(vm, name));
        Assert.False(vm.CanCopy);

        vm.RestoreOffsetsCommand.Execute(null);

        Assert.Equal("4", FontOffset(vm, horizontal));
        Assert.Empty(Errors(vm, name));
        Assert.True(vm.CanCopy);
        Assert.StartsWith("/*", vm.CodeText, StringComparison.Ordinal);
    }

    [Fact]
    public void Valid_offset_text_is_left_unchanged()
    {
        var dialogs = new FakeDialogs();
        FontGeneratorViewModel font = CreateFont(dialogs);
        font.SourceKind = FontSourceKind.TrueType;
        font.OffsetYText = "5";
        int confirms = dialogs.ConfirmCount;

        font.RestoreOffsetsCommand.Execute(null);

        Assert.Equal("5", font.OffsetYText);
        Assert.Equal(confirms, dialogs.ConfirmCount);
    }

    [Fact]
    public void Restoring_an_offset_does_not_mark_parameters_changed()
    {
        var dialogs = new FakeDialogs();
        FontGeneratorViewModel font = CreateFont(dialogs);
        font.SourceKind = FontSourceKind.TrueType;
        font.OffsetYText = "5";
        string fontCaption = font.PresetCaption;
        int fontChanges = 0;
        font.ParametersChanged += () => fontChanges++;
        font.OffsetYText = "x";

        font.RestoreOffsetsCommand.Execute(null);

        Assert.Equal(0, fontChanges);
        Assert.Equal("5", font.OffsetYText);
        Assert.Equal(fontCaption, font.PresetCaption);
    }

    [Fact]
    public void Restore_uses_the_last_accepted_offset_not_the_original()
    {
        FontGeneratorViewModel font = CreateFont();
        font.SourceKind = FontSourceKind.TrueType;
        font.OffsetYText = "3";
        font.OffsetYText = "6";
        font.OffsetYText = "";

        font.RestoreOffsetsCommand.Execute(null);

        Assert.Equal("6", font.OffsetYText);
        Assert.Equal(6, font.ToParameters().GlyphOffsetY);
    }

    [Fact]
    public void Restore_uses_the_offset_loaded_with_parameters()
    {
        FontGeneratorViewModel font = CreateFont();
        FontTabParameters fontParameters = font.ToParameters() with
        {
            SourceKind = FontSourceKind.TrueType,
            Family = "Stub",
            GlyphOffsetX = 4,
            GlyphOffsetY = -4,
        };
        font.Load(new FontProjectTab(fontParameters, new FontTable(font.Cell)));
        font.OffsetYText = "40";

        font.RestoreOffsetsCommand.Execute(null);

        Assert.Equal("4", font.OffsetXText);
        Assert.Equal("-4", font.OffsetYText);
        Assert.Equal(-4, font.ToParameters().GlyphOffsetY);
    }

    [Fact]
    public void Loaded_offset_outside_the_range_is_clamped_before_it_is_stored()
    {
        FontGeneratorViewModel font = CreateFont();
        FontTabParameters fontParameters = font.ToParameters() with
        {
            GlyphOffsetX = 100,
            GlyphOffsetY = -100,
        };
        font.Load(new FontProjectTab(fontParameters, new FontTable(font.Cell)));

        Assert.Equal("4", font.OffsetXText);
        Assert.Equal("-6", font.OffsetYText);
        Assert.Equal(4, font.ToParameters().GlyphOffsetX);
        Assert.Equal(-6, font.ToParameters().GlyphOffsetY);
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

    private static void SetFontOffset(FontGeneratorViewModel vm, bool horizontal, string text)
    {
        if (horizontal)
        {
            vm.OffsetXText = text;
        }
        else
        {
            vm.OffsetYText = text;
        }
    }

    private static string FontOffset(FontGeneratorViewModel vm, bool horizontal) =>
        horizontal ? vm.OffsetXText : vm.OffsetYText;

    private static IReadOnlyList<string> Errors(INotifyDataErrorInfo vm, string property) =>
        vm.GetErrors(property).Cast<object>().Select(error => error.ToString()!).ToArray();

    private FontGeneratorViewModel CreateFont(FakeDialogs? dialogs = null)
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        var vm = new FontGeneratorViewModel(
            new MapText(Text),
            dialogs ?? new FakeDialogs(),
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            new StubOutlines(),
            PresetCatalog.Shared,
            settings.UserPresets,
            settings.Font);
        vm.Family = "Stub";
        return vm;
    }
}
