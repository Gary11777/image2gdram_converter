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

    public static TheoryData<bool, string> ImageBadValues => new()
    {
        { true, "" },
        { true, "   " },
        { true, "-" },
        { true, "abc" },
        { true, "1.5" },
        { true, "1025" },
        { true, "-1025" },
        { false, "" },
        { false, "   " },
        { false, "-" },
        { false, "abc" },
        { false, "1.5" },
        { false, "1025" },
        { false, "-1025" },
    };

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
    [MemberData(nameof(ImageBadValues))]
    public void Image_offset_returns_to_the_last_valid_value(bool horizontal, string bad)
    {
        ImageConverterViewModel vm = CreateImage();
        LoadWhite(vm);
        Assert.True(vm.CanCopy);
        string name = horizontal ? nameof(vm.OffsetXText) : nameof(vm.OffsetYText);

        SetImageOffset(vm, horizontal, "-7");
        SetImageOffset(vm, horizontal, bad);

        Assert.NotEmpty(Errors(vm, name));
        Assert.False(vm.CanCopy);

        vm.RestoreOffsetsCommand.Execute(null);

        Assert.Equal("-7", ImageOffset(vm, horizontal));
        Assert.Empty(Errors(vm, name));
        Assert.True(vm.CanCopy);
        Assert.StartsWith("/*", vm.CodeText, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(FontBadValues))]
    public void Font_offset_returns_to_the_last_valid_value(bool horizontal, string bad)
    {
        FontGeneratorViewModel vm = CreateFont();
        vm.SourceKind = FontSourceKind.TrueType;
        Assert.True(vm.CanCopy);
        string name = horizontal ? nameof(vm.OffsetXText) : nameof(vm.OffsetYText);

        SetFontOffset(vm, horizontal, "5");
        SetFontOffset(vm, horizontal, bad);

        Assert.NotEmpty(Errors(vm, name));
        Assert.False(vm.CanCopy);

        vm.RestoreOffsetsCommand.Execute(null);

        Assert.Equal("5", FontOffset(vm, horizontal));
        Assert.Empty(Errors(vm, name));
        Assert.True(vm.CanCopy);
        Assert.StartsWith("/*", vm.CodeText, StringComparison.Ordinal);
    }

    [Fact]
    public void Valid_offset_text_is_left_unchanged()
    {
        var dialogs = new FakeDialogs();
        ImageConverterViewModel image = CreateImage(dialogs);
        LoadWhite(image);
        image.OffsetXText = "6";
        image.OffsetXText = " 8";
        Assert.Equal(0, dialogs.ConfirmCount);

        image.RestoreOffsetsCommand.Execute(null);

        Assert.Equal(" 8", image.OffsetXText);
        Assert.Equal(0, dialogs.ConfirmCount);

        FontGeneratorViewModel font = CreateFont(dialogs);
        font.SourceKind = FontSourceKind.TrueType;
        font.OffsetYText = "5";
        int confirms = dialogs.ConfirmCount;

        font.RestoreOffsetsCommand.Execute(null);

        Assert.Equal("5", font.OffsetYText);
        Assert.Equal(confirms, dialogs.ConfirmCount);
    }

    [Fact]
    public void Restoring_an_offset_does_not_ask_to_drop_pixel_edits()
    {
        var dialogs = new FakeDialogs { ConfirmAnswer = false };
        ImageConverterViewModel vm = CreateImage(dialogs);
        LoadWhite(vm);
        vm.OffsetXText = "-7";
        vm.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);
        Assert.True(vm.HasEdits);
        Assert.True(vm.CanUndo);
        int confirms = dialogs.ConfirmCount;

        vm.OffsetXText = "";
        vm.RestoreOffsetsCommand.Execute(null);

        Assert.Equal(confirms, dialogs.ConfirmCount);
        Assert.Equal(0, dialogs.ConfirmCount);
        Assert.Equal("-7", vm.OffsetXText);
        Assert.True(vm.HasEdits);
        Assert.True(vm.CanUndo);
    }

    [Fact]
    public void Restoring_an_offset_does_not_mark_parameters_changed()
    {
        ImageConverterViewModel image = CreateImage();
        LoadWhite(image);
        image.OffsetXText = "-7";
        string imageCaption = image.PresetCaption;
        int imageChanges = 0;
        image.ParametersChanged += () => imageChanges++;
        image.OffsetXText = "abc";

        image.RestoreOffsetsCommand.Execute(null);

        Assert.Equal(0, imageChanges);
        Assert.Equal("-7", image.OffsetXText);
        Assert.Equal(imageCaption, image.PresetCaption);

        FontGeneratorViewModel font = CreateFont();
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
        ImageConverterViewModel image = CreateImage();
        LoadWhite(image);
        image.OffsetXText = "3";
        image.OffsetXText = "12";
        image.OffsetXText = "";

        image.RestoreOffsetsCommand.Execute(null);

        Assert.Equal("12", image.OffsetXText);
        Assert.Equal(12, image.ToParameters().Processing.OffsetX);

        FontGeneratorViewModel font = CreateFont();
        font.SourceKind = FontSourceKind.TrueType;
        font.OffsetYText = "3";
        font.OffsetYText = "12";
        font.OffsetYText = "";

        font.RestoreOffsetsCommand.Execute(null);

        Assert.Equal("12", font.OffsetYText);
        Assert.Equal(12, font.ToParameters().GlyphOffsetY);
    }

    [Fact]
    public void Restore_uses_the_offset_loaded_with_parameters()
    {
        ImageConverterViewModel image = CreateImage();
        LoadWhite(image);
        ImageTabParameters imageParameters = image.ToParameters() with
        {
            Processing = image.ToParameters().Processing with { OffsetX = 15, OffsetY = -9 },
        };
        image.LoadState(imageParameters, schedule: true);
        image.OffsetXText = "2000";

        image.RestoreOffsetsCommand.Execute(null);

        Assert.Equal("15", image.OffsetXText);
        Assert.Equal("-9", image.OffsetYText);
        Assert.Equal(15, image.ToParameters().Processing.OffsetX);

        FontGeneratorViewModel font = CreateFont();
        FontTabParameters fontParameters = font.ToParameters() with
        {
            SourceKind = FontSourceKind.TrueType,
            Family = "Stub",
            GlyphOffsetX = 9,
            GlyphOffsetY = -4,
        };
        font.Load(new FontProjectTab(fontParameters, new FontTable(font.Cell)));
        font.OffsetYText = "40";

        font.RestoreOffsetsCommand.Execute(null);

        Assert.Equal("9", font.OffsetXText);
        Assert.Equal("-4", font.OffsetYText);
        Assert.Equal(-4, font.ToParameters().GlyphOffsetY);
    }

    [Fact]
    public void Loaded_offset_outside_the_range_is_clamped_before_it_is_stored()
    {
        ImageConverterViewModel image = CreateImage();
        ImageTabParameters imageParameters = image.ToParameters() with
        {
            Processing = image.ToParameters().Processing with { OffsetX = 5000, OffsetY = -5000 },
        };
        image.LoadState(imageParameters, schedule: false);

        Assert.Equal("1024", image.OffsetXText);
        Assert.Equal("-1024", image.OffsetYText);
        Assert.Equal(1024, image.ToParameters().Processing.OffsetX);
        Assert.Equal(-1024, image.ToParameters().Processing.OffsetY);

        FontGeneratorViewModel font = CreateFont();
        FontTabParameters fontParameters = font.ToParameters() with
        {
            GlyphOffsetX = 100,
            GlyphOffsetY = -100,
        };
        font.Load(new FontProjectTab(fontParameters, new FontTable(font.Cell)));

        Assert.Equal("32", font.OffsetXText);
        Assert.Equal("-32", font.OffsetYText);
        Assert.Equal(32, font.ToParameters().GlyphOffsetX);
        Assert.Equal(-32, font.ToParameters().GlyphOffsetY);
    }

    [Fact]
    public void Lost_focus_restores_offset_and_pending_delay_does_not_clear_it()
    {
        using var ui = new StaDispatcher();
        ImageConverterViewModel vm = CreateImage();
        LoadWhite(vm);
        vm.OffsetXText = "-7";
        TextBox? box = null;
        HwndSource? source = null;
        try
        {
            ui.Dispatcher.Invoke(() =>
            {
                box = new TextBox();
                source = new HwndSource(new HwndSourceParameters("i2g-offset")
                {
                    Width = 20,
                    Height = 20,
                    PositionX = -32000,
                    PositionY = -32000,
                    WindowStyle = unchecked((int)0x80000000),
                })
                {
                    RootVisual = box,
                };
                BindingOperations.SetBinding(
                    box,
                    TextBox.TextProperty,
                    new Binding(nameof(ImageConverterViewModel.OffsetXText))
                    {
                        Source = vm,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        Delay = 400,
                    });
                TextBoxCommit.SetCommand(box, vm.RestoreOffsetsCommand);
                Assert.NotNull(BindingOperations.GetBindingExpression(box, TextBox.TextProperty));
                Assert.Equal("-7", box.Text);

                box.Text = "";
                Assert.Equal("-7", vm.OffsetXText);

                box.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, box, null)
                {
                    RoutedEvent = Keyboard.LostKeyboardFocusEvent,
                });

                Assert.Equal("-7", box.Text);
                Assert.Equal("-7", vm.OffsetXText);
                Assert.True(vm.CanCopy);
            });

            Thread.Sleep(700);
            ui.Flush();
            ui.Dispatcher.Invoke(() =>
            {
                Assert.Equal("-7", box!.Text);
                Assert.Equal("-7", vm.OffsetXText);
                Assert.Empty(Errors(vm, nameof(vm.OffsetXText)));
                Assert.True(vm.CanCopy);
                Assert.StartsWith("/*", vm.CodeText, StringComparison.Ordinal);
            });
        }
        finally
        {
            ui.Dispatcher.Invoke(() => source?.Dispose());
        }
    }

    [Fact]
    public void A_new_number_typed_before_the_delay_replaces_the_cleared_value()
    {
        using var ui = new StaDispatcher();
        ImageConverterViewModel vm = CreateImage();
        LoadWhite(vm);
        vm.OffsetXText = "-7";
        TextBox? box = null;
        HwndSource? source = null;
        try
        {
            ui.Dispatcher.Invoke(() =>
            {
                box = new TextBox();
                source = new HwndSource(new HwndSourceParameters("i2g-offset-type")
                {
                    Width = 20,
                    Height = 20,
                    PositionX = -32000,
                    PositionY = -32000,
                    WindowStyle = unchecked((int)0x80000000),
                })
                {
                    RootVisual = box,
                };
                BindingOperations.SetBinding(
                    box,
                    TextBox.TextProperty,
                    new Binding(nameof(ImageConverterViewModel.OffsetXText))
                    {
                        Source = vm,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        Delay = 400,
                    });
                TextBoxCommit.SetCommand(box, vm.RestoreOffsetsCommand);
                box.Text = "";
                box.Text = "12";
                Assert.Equal("-7", vm.OffsetXText);

                box.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, box, null)
                {
                    RoutedEvent = Keyboard.LostKeyboardFocusEvent,
                });

                Assert.Equal("12", box.Text);
                Assert.Equal("12", vm.OffsetXText);
            });

            Thread.Sleep(700);
            ui.Flush();
            ui.Dispatcher.Invoke(() =>
            {
                Assert.Equal("12", box!.Text);
                Assert.Equal("12", vm.OffsetXText);
                Assert.Equal(12, vm.ToParameters().Processing.OffsetX);
                Assert.Empty(Errors(vm, nameof(vm.OffsetXText)));
            });
        }
        finally
        {
            ui.Dispatcher.Invoke(() => source?.Dispose());
        }
    }

    [Fact]
    public void Enter_restores_an_invalid_offset_without_marking_the_key_handled()
    {
        using var ui = new StaDispatcher();
        ImageConverterViewModel vm = CreateImage();
        LoadWhite(vm);
        vm.OffsetYText = "4";
        ui.Dispatcher.Invoke(() =>
        {
            var box = new TextBox();
            using var source = new HwndSource(new HwndSourceParameters("i2g-offset-enter")
            {
                Width = 20,
                Height = 20,
                PositionX = -32000,
                PositionY = -32000,
                WindowStyle = unchecked((int)0x80000000),
            })
            {
                RootVisual = box,
            };
            BindingOperations.SetBinding(
                box,
                TextBox.TextProperty,
                new Binding(nameof(ImageConverterViewModel.OffsetYText))
                {
                    Source = vm,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Delay = 400,
                });
            TextBoxCommit.SetCommand(box, vm.RestoreOffsetsCommand);
            box.Text = "2000";
            Assert.Equal("4", vm.OffsetYText);

            var key = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
            {
                RoutedEvent = Keyboard.PreviewKeyDownEvent,
            };
            box.RaiseEvent(key);

            Assert.False(key.Handled);
            Assert.Equal("4", box.Text);
            Assert.Equal("4", vm.OffsetYText);
            Assert.Empty(Errors(vm, nameof(vm.OffsetYText)));
            Assert.True(vm.CanCopy);
        });
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

    private static void SetImageOffset(ImageConverterViewModel vm, bool horizontal, string text)
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

    private static string ImageOffset(ImageConverterViewModel vm, bool horizontal) =>
        horizontal ? vm.OffsetXText : vm.OffsetYText;

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

    private static void LoadWhite(ImageConverterViewModel vm)
    {
        var image = new RgbaImage(8, 8, 255, 255, 255, 255);
        vm.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { image }), "white.png", resetFrame: true, userAction: false);
        vm.ArrayName = "logo";
    }

    private ImageConverterViewModel CreateImage(FakeDialogs? dialogs = null)
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        return new ImageConverterViewModel(
            new MapText(Text),
            dialogs ?? new FakeDialogs(),
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            PresetCatalog.Shared,
            settings.UserPresets,
            ImageTabParameters.CreateDefault());
    }

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
