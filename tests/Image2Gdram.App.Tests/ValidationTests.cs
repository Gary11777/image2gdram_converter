using System.ComponentModel;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Settings;
using image2gdram_converter;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.App.Tests;

/// <summary>П. 6 ТЗ: недопустимое значение сразу подсвечивается, генерация, копирование и сохранение с ним невозможны.</summary>
public class ValidationTests : IDisposable
{
    private static readonly Dictionary<string, string> Text = TestFiles.LoadUiStrings();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-valid-" + Guid.NewGuid().ToString("N"));

    public ValidationTests() => Directory.CreateDirectory(_directory);

    public static TheoryData<string, string, string, string> ImageFields => new()
    {
        { nameof(ImageConverterViewModel.WidthText), "0", "128", "Error.Width" },
        { nameof(ImageConverterViewModel.WidthText), "1025", "128", "Error.Width" },
        { nameof(ImageConverterViewModel.WidthText), "12a", "128", "Error.Width" },
        { nameof(ImageConverterViewModel.HeightText), "", "64", "Error.Height" },
        { nameof(ImageConverterViewModel.HeightText), "-1", "64", "Error.Height" },
        { nameof(ImageConverterViewModel.OffsetXText), "1025", "0", "Error.Offset" },
        { nameof(ImageConverterViewModel.OffsetYText), "-1025", "0", "Error.Offset" },
        { nameof(ImageConverterViewModel.ThresholdText), "256", "128", "Error.Threshold" },
        { nameof(ImageConverterViewModel.ThresholdText), "x", "128", "Error.Threshold" },
        { nameof(ImageConverterViewModel.BytesPerLineText), "0", "16", "Error.BytesPerLine" },
        { nameof(ImageConverterViewModel.BytesPerLineText), "17", "16", "Error.BytesPerLine" },
        { nameof(ImageConverterViewModel.ArrayName), "", "logo", "Error.Name.Empty" },
        { nameof(ImageConverterViewModel.ArrayName), "1logo", "logo", "Error.Name.StartsWithDigit" },
        { nameof(ImageConverterViewModel.ArrayName), "лого", "logo", "Error.Name.InvalidCharacter" },
        { nameof(ImageConverterViewModel.ArrayName), "int", "logo", "Error.Name.CKeyword" },
        { nameof(ImageConverterViewModel.ArrayName), "xdata", "logo", "Error.Name.Keil" },
    };

    public static TheoryData<FontSourceKind, string, string, string, string> FontFields => new()
    {
        { FontSourceKind.TrueType, nameof(FontGeneratorViewModel.FontSizeText), "0", "8", "Error.FontSize" },
        { FontSourceKind.TrueType, nameof(FontGeneratorViewModel.FontSizeText), "65", "8", "Error.FontSize" },
        { FontSourceKind.TrueType, nameof(FontGeneratorViewModel.OffsetXText), "33", "0", "Error.GlyphOffset" },
        { FontSourceKind.TrueType, nameof(FontGeneratorViewModel.OffsetYText), "-33", "0", "Error.GlyphOffset" },
        { FontSourceKind.TrueType, nameof(FontGeneratorViewModel.GlyphThresholdText), "300", "128", "Error.Threshold" },
        { FontSourceKind.Sheet, nameof(FontGeneratorViewModel.SheetWidthText), "0", "6", "Error.SheetCell" },
        { FontSourceKind.Sheet, nameof(FontGeneratorViewModel.SheetHeightText), "65", "8", "Error.SheetCell" },
        { FontSourceKind.Sheet, nameof(FontGeneratorViewModel.MarginXText), "-1", "0", "Error.Margin" },
        { FontSourceKind.Sheet, nameof(FontGeneratorViewModel.MarginYText), "1025", "0", "Error.Margin" },
        { FontSourceKind.Sheet, nameof(FontGeneratorViewModel.SpacingXText), "257", "0", "Error.Spacing" },
        { FontSourceKind.Sheet, nameof(FontGeneratorViewModel.SpacingYText), "-1", "0", "Error.Spacing" },
        { FontSourceKind.Sheet, nameof(FontGeneratorViewModel.CharsPerRowText), "0", "16", "Error.CharsPerRow" },
        { FontSourceKind.Sheet, nameof(FontGeneratorViewModel.FirstCodeText), "256", "0", "Error.FirstCode" },
        { FontSourceKind.Sheet, nameof(FontGeneratorViewModel.SheetThresholdText), "-5", "128", "Error.Threshold" },
        { FontSourceKind.Manual, nameof(FontGeneratorViewModel.BytesPerLineText), "17", "16", "Error.BytesPerLine" },
        { FontSourceKind.Manual, nameof(FontGeneratorViewModel.ArrayName), "char", "font", "Error.Name.CKeyword" },
        { FontSourceKind.Manual, nameof(FontGeneratorViewModel.ArrayName), "9font", "font", "Error.Name.StartsWithDigit" },
    };

    [Theory]
    [MemberData(nameof(ImageFields))]
    public void Image_field_error_is_shown_at_once_blocks_output_and_clears_when_fixed(string property, string bad, string good, string key)
    {
        ImageConverterViewModel vm = CreateImage();
        LoadWhite(vm);
        Assert.True(vm.CanCopy);
        var raised = new List<string?>();
        vm.ErrorsChanged += (_, args) => raised.Add(args.PropertyName);

        Set(vm, property, bad);

        Assert.Contains(property, raised);
        string message = Assert.Single(Errors(vm, property));
        Assert.Equal(Text[key], message);
        Assert.False(vm.CanCopy);
        Assert.False(vm.CanSaveOutput);
        Assert.False(vm.CopyCommand.CanExecute(null));
        Assert.False(vm.SaveOutputCommand.CanExecute(null));
        Assert.Equal(message, vm.CodeText);

        Set(vm, property, good);

        Assert.Empty(Errors(vm, property));
        Assert.True(vm.CanCopy);
        Assert.True(vm.CopyCommand.CanExecute(null));
        Assert.StartsWith("/*", vm.CodeText, StringComparison.Ordinal);
    }

    [Fact]
    public void Name_valid_in_c_becomes_invalid_when_the_format_switches_to_an_a51_module()
    {
        ImageConverterViewModel vm = CreateImage();
        LoadWhite(vm);
        vm.ArrayName = new string('a', 30);
        Assert.True(vm.CanCopy);

        vm.Format = OutputFormat.A51Module;

        Assert.Equal(string.Format(Text["Error.Name.TooLong"], 27), Assert.Single(Errors(vm, nameof(vm.ArrayName))));
        Assert.False(vm.CanCopy);

        vm.Format = OutputFormat.CStm32;

        Assert.Empty(Errors(vm, nameof(vm.ArrayName)));
        Assert.True(vm.CanCopy);
    }

    [Fact]
    public void Size_fields_are_not_checked_when_the_size_follows_the_source()
    {
        ImageConverterViewModel vm = CreateImage();
        LoadWhite(vm);
        vm.WidthText = "0";
        Assert.False(vm.CanCopy);

        vm.SizeMode = Image2Gdram.Core.Processing.TargetSizeMode.MatchSource;

        Assert.Empty(Errors(vm, nameof(vm.WidthText)));
        Assert.True(vm.CanCopy);
    }

    [Fact]
    public void Invalid_field_blocks_painting_in_the_grid()
    {
        ImageConverterViewModel vm = CreateImage();
        LoadWhite(vm);
        vm.ThresholdText = "999";

        vm.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);

        Assert.False(vm.HasEdits);
    }

    [Theory]
    [MemberData(nameof(FontFields))]
    public void Font_field_error_is_shown_at_once_blocks_output_and_clears_when_fixed(FontSourceKind kind, string property, string bad, string good, string key)
    {
        FontGeneratorViewModel vm = CreateFont();
        vm.SourceKind = kind;
        Assert.True(vm.CanCopy);

        Set(vm, property, bad);

        string message = Assert.Single(Errors(vm, property));
        Assert.Equal(Text[key], message);
        Assert.False(vm.CanCopy);
        Assert.False(vm.CopyCommand.CanExecute(null));
        Assert.False(vm.SaveOutputCommand.CanExecute(null));
        Assert.Equal(message, vm.CodeText);

        Set(vm, property, good);

        Assert.Empty(Errors(vm, property));
        Assert.True(vm.CanCopy);
    }

    [Theory]
    [InlineData("0x41-0x30", "Error.Range.Order")]
    [InlineData("0x100", "Error.Range.Bounds")]
    [InlineData("0x41, zz", "Error.Range.Number")]
    [InlineData("0x41--0x42", "Error.Range.Dash")]
    public void Wrong_character_codes_are_shown_with_position_and_block_output(string range, string key)
    {
        FontGeneratorViewModel vm = CreateFont();
        vm.SourceKind = FontSourceKind.TrueType;
        Assert.True(vm.CanCopy);

        vm.CustomRangeText = range;

        string message = Assert.Single(Errors(vm, nameof(vm.CustomRangeText)));
        string prefix = Text[key][..Text[key].IndexOf('{')];
        Assert.StartsWith(prefix, message, StringComparison.Ordinal);
        Assert.False(vm.CanCopy);

        vm.CustomRangeText = "0x41-0x42";

        Assert.Empty(Errors(vm, nameof(vm.CustomRangeText)));
        Assert.True(vm.CanCopy);
    }

    [Fact]
    public void Fields_of_a_hidden_source_do_not_block_output()
    {
        FontGeneratorViewModel vm = CreateFont();
        vm.SourceKind = FontSourceKind.Sheet;
        vm.FirstCodeText = "300";
        Assert.False(vm.CanCopy);
        vm.SourceKind = FontSourceKind.TrueType;
        vm.CustomRangeText = "0x300";
        Assert.False(vm.CanCopy);

        vm.SourceKind = FontSourceKind.Manual;

        Assert.False(vm.HasErrors);
        Assert.True(vm.CanCopy);

        vm.SourceKind = FontSourceKind.TrueType;

        Assert.Single(Errors(vm, nameof(vm.CustomRangeText)));
        Assert.Empty(Errors(vm, nameof(vm.FirstCodeText)));
        Assert.False(vm.CanCopy);
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

    private static void Set(object vm, string property, string value) =>
        vm.GetType().GetProperty(property)!.SetValue(vm, value);

    private static IReadOnlyList<string> Errors(INotifyDataErrorInfo vm, string property) =>
        vm.GetErrors(property).Cast<object>().Select(error => error.ToString()!).ToArray();

    private static void LoadWhite(ImageConverterViewModel vm)
    {
        var image = new RgbaImage(8, 8, 255, 255, 255, 255);
        vm.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { image }), "white.png", resetFrame: true, userAction: false);
        vm.ArrayName = "logo";
    }

    private ImageConverterViewModel CreateImage()
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        return new ImageConverterViewModel(
            new MapText(Text),
            new FakeDialogs(),
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            PresetCatalog.Shared,
            settings.UserPresets,
            ImageTabParameters.CreateDefault());
    }

    private FontGeneratorViewModel CreateFont()
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        var vm = new FontGeneratorViewModel(
            new MapText(Text),
            new FakeDialogs(),
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
