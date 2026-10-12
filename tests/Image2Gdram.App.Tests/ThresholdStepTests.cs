using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Projects;
using Image2Gdram.Core.Settings;
using image2gdram_converter;
using image2gdram_converter.Controls;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.App.Tests;

/// <summary>N-68: minus and plus buttons on the three threshold fields.</summary>
public class ThresholdStepTests : IDisposable
{
    private static readonly Dictionary<string, string> Text = TestFiles.LoadUiStrings();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-threshold-" + Guid.NewGuid().ToString("N"));

    public ThresholdStepTests() => Directory.CreateDirectory(_directory);

    public enum Kind
    {
        Image,
        Glyph,
        Sheet,
    }

    public static TheoryData<Kind> Kinds => new()
    {
        Kind.Image,
        Kind.Glyph,
        Kind.Sheet,
    };

    public static TheoryData<Kind, string> BadText => new()
    {
        { Kind.Image, "abc" },
        { Kind.Image, "" },
        { Kind.Image, "300" },
        { Kind.Glyph, "abc" },
        { Kind.Glyph, "" },
        { Kind.Glyph, "300" },
        { Kind.Sheet, "abc" },
        { Kind.Sheet, "" },
        { Kind.Sheet, "300" },
    };

    public static TheoryData<int> Boundaries => new() { 0, 255 };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Step_changes_the_value_by_one_and_recalculates(Kind kind)
    {
        using Session session = Create();
        PrepareInk(session, kind);
        SetThreshold(session, kind, 0);
        Assert.Equal(0, Ink(session, kind));
        string code = Code(session, kind);

        Increase(session, kind).Execute(null);

        Assert.Equal(1, Read(session, kind));
        Assert.Equal("1", ReadText(session, kind));
        Assert.True(Ink(session, kind) > 0);
        Assert.NotEqual(code, Code(session, kind));

        Decrease(session, kind).Execute(null);

        Assert.Equal(0, Read(session, kind));
        Assert.Equal("0", ReadText(session, kind));
        Assert.Equal(0, Ink(session, kind));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Bounds_disable_the_matching_command_and_a_step_raises_CanExecuteChanged(Kind kind)
    {
        using Session session = Create();
        PrepareInk(session, kind);

        SetThreshold(session, kind, 0);
        Assert.False(Decrease(session, kind).CanExecute(null));
        Assert.True(Increase(session, kind).CanExecute(null));
        Decrease(session, kind).Execute(null);
        Assert.Equal(0, Read(session, kind));

        SetThreshold(session, kind, 1);
        Assert.True(Decrease(session, kind).CanExecute(null));
        int decreaseEvents = 0;
        Decrease(session, kind).CanExecuteChanged += (_, _) => decreaseEvents++;
        Decrease(session, kind).Execute(null);
        Assert.Equal(0, Read(session, kind));
        Assert.True(decreaseEvents > 0);
        Assert.False(Decrease(session, kind).CanExecute(null));

        SetThreshold(session, kind, 254);
        Assert.True(Increase(session, kind).CanExecute(null));
        int increaseEvents = 0;
        Increase(session, kind).CanExecuteChanged += (_, _) => increaseEvents++;
        Increase(session, kind).Execute(null);
        Assert.Equal(255, Read(session, kind));
        Assert.Equal("255", ReadText(session, kind));
        Assert.True(increaseEvents > 0);
        Assert.False(Increase(session, kind).CanExecute(null));
        Increase(session, kind).Execute(null);
        Assert.Equal(255, Read(session, kind));
    }

    [Theory]
    [MemberData(nameof(Boundaries))]
    public void Loading_a_boundary_refreshes_the_commands(int boundary)
    {
        using Session session = Create();
        ImageConverterViewModel image = session.Image;
        int imageEvents = 0;
        image.DecreaseThresholdCommand.CanExecuteChanged += (_, _) => imageEvents++;
        image.IncreaseThresholdCommand.CanExecuteChanged += (_, _) => imageEvents++;
        ImageTabParameters imageParameters = image.ToParameters();
        image.LoadState(imageParameters with { Processing = imageParameters.Processing with { Threshold = boundary } }, true);
        Assert.Equal(boundary, image.Threshold);
        Assert.True(imageEvents > 0);
        Assert.Equal(boundary > 0, image.DecreaseThresholdCommand.CanExecute(null));
        Assert.Equal(boundary < 255, image.IncreaseThresholdCommand.CanExecute(null));

        imageEvents = 0;
        image.Presets.Selected = image.Presets.Items.First(item => item.Preset?.Name == "OLED128X64-0.96");
        Assert.Equal(boundary, image.Threshold);
        Assert.True(imageEvents > 0);
        Assert.Equal(boundary > 0, image.DecreaseThresholdCommand.CanExecute(null));
        Assert.Equal(boundary < 255, image.IncreaseThresholdCommand.CanExecute(null));

        FontGeneratorViewModel font = session.Font;
        int fontEvents = 0;
        font.DecreaseGlyphThresholdCommand.CanExecuteChanged += (_, _) => fontEvents++;
        font.IncreaseGlyphThresholdCommand.CanExecuteChanged += (_, _) => fontEvents++;
        font.DecreaseSheetThresholdCommand.CanExecuteChanged += (_, _) => fontEvents++;
        font.IncreaseSheetThresholdCommand.CanExecuteChanged += (_, _) => fontEvents++;
        FontTabParameters fontParameters = font.ToParameters();
        font.Load(new FontProjectTab(
            fontParameters with
            {
                GlyphThreshold = boundary,
                Sheet = fontParameters.Sheet with { Threshold = boundary },
            },
            new FontTable(font.Cell)));
        Assert.Equal(boundary, font.GlyphThreshold);
        Assert.Equal(boundary, font.SheetThreshold);
        Assert.True(fontEvents > 0);
        Assert.Equal(boundary > 0, font.DecreaseGlyphThresholdCommand.CanExecute(null));
        Assert.Equal(boundary < 255, font.IncreaseGlyphThresholdCommand.CanExecute(null));
        Assert.Equal(boundary > 0, font.DecreaseSheetThresholdCommand.CanExecute(null));
        Assert.Equal(boundary < 255, font.IncreaseSheetThresholdCommand.CanExecute(null));

        fontEvents = 0;
        font.Presets.Selected = font.Presets.Items.First(item => item.Preset?.Name == "WG240128A");
        Assert.Equal(boundary, font.GlyphThreshold);
        Assert.Equal(boundary, font.SheetThreshold);
        Assert.True(fontEvents > 0);
        Assert.Equal(boundary < 255, font.IncreaseGlyphThresholdCommand.CanExecute(null));
        Assert.Equal(boundary < 255, font.IncreaseSheetThresholdCommand.CanExecute(null));
    }

    [Theory]
    [MemberData(nameof(BadText))]
    public void Invalid_text_steps_from_the_last_accepted_value(Kind kind, string bad)
    {
        using Session session = Create();
        PrepareOutput(session, kind);
        SetThreshold(session, kind, 40);
        SetText(session, kind, bad);
        Assert.NotEmpty(Errors(session, kind));
        Assert.False(CanGenerate(session, kind));

        Increase(session, kind).Execute(null);

        Assert.Equal(41, Read(session, kind));
        Assert.Equal("41", ReadText(session, kind));
        Assert.Empty(Errors(session, kind));
        Assert.True(CanGenerate(session, kind));
        Assert.True(CanSave(session, kind));
    }

    [Fact]
    public void Refusing_the_reset_keeps_the_threshold_and_the_edit()
    {
        using Session session = Create();
        ImageConverterViewModel vm = session.Image;
        LoadBlackDot(vm);
        vm.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);
        session.Dialogs.ConfirmAnswer = false;

        vm.IncreaseThresholdCommand.Execute(null);

        Assert.Equal(1, session.Dialogs.ConfirmCount);
        Assert.Equal(128, vm.Threshold);
        Assert.Equal("128", vm.ThresholdText);
        Assert.True(vm.HasEdits);
        Assert.True(vm.Preview![1, 1]);
    }

    [Fact]
    public void Accepting_the_reset_applies_the_step_and_drops_the_edit()
    {
        using Session session = Create();
        ImageConverterViewModel vm = session.Image;
        LoadBlackDot(vm);
        vm.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);
        session.Dialogs.ConfirmAnswer = true;

        vm.IncreaseThresholdCommand.Execute(null);

        Assert.Equal(1, session.Dialogs.ConfirmCount);
        Assert.Equal(129, vm.Threshold);
        Assert.False(vm.HasEdits);
        Assert.False(vm.Preview![1, 1]);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Step_leaves_the_preset_title_as_the_slider_does(Kind kind)
    {
        using Session session = Create();
        PrepareOutput(session, kind);
        const string name = "OLED128X64-0.96";
        SelectPreset(session, kind, name);
        Assert.Equal(name, Caption(session, kind));

        int slider = Read(session, kind);
        SetThreshold(session, kind, slider + 1);
        string afterSlider = Caption(session, kind);

        SelectPreset(session, kind, name);
        Increase(session, kind).Execute(null);

        Assert.Equal(afterSlider, Caption(session, kind));
        Assert.Equal(name, Caption(session, kind));
    }

    [Fact]
    public void Typing_a_number_and_focusing_increase_uses_that_number()
    {
        using var ui = new StaDispatcher();
        using Session session = Create();
        ImageConverterViewModel vm = session.Image;
        LoadBlackDot(vm);
        Window? window = null;
        try
        {
            ui.Dispatcher.Invoke(() =>
            {
                var box = new TextBox { Width = 64 };
                var button = new Button
                {
                    Content = "+",
                    MinWidth = 22,
                    Focusable = true,
                    IsTabStop = false,
                    Command = vm.IncreaseThresholdCommand,
                };
                var panel = new StackPanel();
                panel.Children.Add(box);
                panel.Children.Add(button);
                window = new Window
                {
                    Width = 220,
                    Height = 120,
                    Left = -20000,
                    Top = -20000,
                    ShowInTaskbar = false,
                    ShowActivated = true,
                    WindowStyle = WindowStyle.None,
                    Content = panel,
                };
                window.Show();
                BindingOperations.SetBinding(
                    box,
                    TextBox.TextProperty,
                    new Binding(nameof(ImageConverterViewModel.ThresholdText))
                    {
                        Source = vm,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        Delay = 400,
                    });
                TextBoxCommit.SetFlush(box, true);
                box.Focus();
                box.Text = "200";
                Assert.Equal(128, vm.Threshold);

                button.Focus();

                Assert.True(button.IsKeyboardFocused);
                Assert.Equal(200, vm.Threshold);
                Assert.Equal("200", vm.ThresholdText);
                button.Command.Execute(null);
                Assert.Equal(201, vm.Threshold);
                Assert.Equal("201", box.Text);
            });

            Thread.Sleep(700);
            ui.Flush();
            ui.Dispatcher.Invoke(() =>
            {
                Assert.Equal(201, vm.Threshold);
                Assert.Equal("201", vm.ThresholdText);
                Assert.Equal(201, vm.ToParameters().Processing.Threshold);
            });
        }
        finally
        {
            ui.Dispatcher.Invoke(() => window?.Close());
        }
    }

    [Fact]
    public void Flush_pushes_text_on_lost_focus_and_enter_without_a_command()
    {
        using var ui = new StaDispatcher();
        ui.Dispatcher.Invoke(() =>
        {
            var holder = new TextHolder { Text = "1" };
            var box = new TextBox();
            using var source = new HwndSource(new HwndSourceParameters("i2g-threshold-flush")
            {
                Width = 40,
                Height = 40,
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
                new Binding(nameof(TextHolder.Text))
                {
                    Source = holder,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Delay = 400,
                });
            box.Text = "200";
            RaiseLostFocus(box);
            Assert.Equal("1", holder.Text);

            TextBoxCommit.SetFlush(box, true);
            Assert.Null(TextBoxCommit.GetCommand(box));
            box.Text = "200";
            Assert.Equal("1", holder.Text);
            RaiseLostFocus(box);
            Assert.Equal("200", holder.Text);

            box.Text = "9";
            var key = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
            {
                RoutedEvent = Keyboard.PreviewKeyDownEvent,
            };
            box.RaiseEvent(key);
            Assert.False(key.Handled);
            Assert.Equal("9", holder.Text);
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

    private static void RaiseLostFocus(TextBox box) =>
        box.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, box, null)
        {
            RoutedEvent = Keyboard.LostKeyboardFocusEvent,
        });

    private void PrepareInk(Session session, Kind kind)
    {
        switch (kind)
        {
            case Kind.Image:
                LoadBlackDot(session.Image);
                break;
            case Kind.Glyph:
                UseGlyph(session.Font);
                break;
            case Kind.Sheet:
                LoadBlackSheet(session.Font);
                break;
        }
    }

    private void PrepareOutput(Session session, Kind kind)
    {
        switch (kind)
        {
            case Kind.Image:
                LoadBlackDot(session.Image);
                break;
            case Kind.Glyph:
                session.Font.SourceKind = FontSourceKind.TrueType;
                session.Font.Family = "Stub";
                session.Font.RenderMode = GlyphRenderMode.Antialiased;
                break;
            case Kind.Sheet:
                LoadBlackSheet(session.Font);
                break;
        }

        Assert.True(CanGenerate(session, kind));
    }

    private void LoadBlackSheet(FontGeneratorViewModel vm)
    {
        vm.Latin = false;
        vm.Cyrillic = false;
        vm.CustomRangeText = "0x00";
        var image = new RgbaImage(6, 8, 255, 255, 255, 255);
        image.SetPixel(0, 0, 0, 0, 0, 255);
        vm.LoadSheet(image, Path.Combine(_directory, "sheet.png"));
    }

    private static void UseGlyph(FontGeneratorViewModel vm)
    {
        vm.SourceKind = FontSourceKind.TrueType;
        vm.Family = "Stub";
        vm.RenderMode = GlyphRenderMode.Antialiased;
        vm.Latin = false;
        vm.Cyrillic = false;
        vm.CustomRangeText = "0x41";
    }

    private static void LoadBlackDot(ImageConverterViewModel vm)
    {
        vm.WidthText = "8";
        vm.HeightText = "8";
        vm.Fit = FitMode.None;
        var image = new RgbaImage(8, 8, 255, 255, 255, 255);
        image.SetPixel(0, 0, 0, 0, 0, 255);
        vm.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { image }), "dot.png", resetFrame: true, userAction: false);
    }

    private static void SelectPreset(Session session, Kind kind, string name)
    {
        PresetSelectorViewModel presets = kind == Kind.Image ? session.Image.Presets : session.Font.Presets;
        presets.Selected = presets.Items.First(item => item.Preset?.Name == name);
    }

    private static ICommand Increase(Session session, Kind kind) => kind switch
    {
        Kind.Image => session.Image.IncreaseThresholdCommand,
        Kind.Glyph => session.Font.IncreaseGlyphThresholdCommand,
        Kind.Sheet => session.Font.IncreaseSheetThresholdCommand,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static ICommand Decrease(Session session, Kind kind) => kind switch
    {
        Kind.Image => session.Image.DecreaseThresholdCommand,
        Kind.Glyph => session.Font.DecreaseGlyphThresholdCommand,
        Kind.Sheet => session.Font.DecreaseSheetThresholdCommand,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static int Read(Session session, Kind kind) => kind switch
    {
        Kind.Image => session.Image.Threshold,
        Kind.Glyph => session.Font.GlyphThreshold,
        Kind.Sheet => session.Font.SheetThreshold,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static string ReadText(Session session, Kind kind) => kind switch
    {
        Kind.Image => session.Image.ThresholdText,
        Kind.Glyph => session.Font.GlyphThresholdText,
        Kind.Sheet => session.Font.SheetThresholdText,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static void SetThreshold(Session session, Kind kind, int value)
    {
        switch (kind)
        {
            case Kind.Image:
                session.Image.Threshold = value;
                break;
            case Kind.Glyph:
                session.Font.GlyphThreshold = value;
                break;
            case Kind.Sheet:
                session.Font.SheetThreshold = value;
                break;
        }
    }

    private static void SetText(Session session, Kind kind, string value)
    {
        switch (kind)
        {
            case Kind.Image:
                session.Image.ThresholdText = value;
                break;
            case Kind.Glyph:
                session.Font.GlyphThresholdText = value;
                break;
            case Kind.Sheet:
                session.Font.SheetThresholdText = value;
                break;
        }
    }

    private static string Caption(Session session, Kind kind) =>
        kind == Kind.Image ? session.Image.PresetCaption : session.Font.PresetCaption;

    private static string Code(Session session, Kind kind) =>
        kind == Kind.Image ? session.Image.CodeText : session.Font.CodeText;

    private static bool CanGenerate(Session session, Kind kind) =>
        kind == Kind.Image ? session.Image.CanCopy : session.Font.CanCopy;

    private static bool CanSave(Session session, Kind kind) =>
        kind == Kind.Image ? session.Image.CanSaveOutput : session.Font.CanSaveOutput;

    private static int Ink(Session session, Kind kind)
    {
        MonoBitmap? bitmap = kind switch
        {
            Kind.Image => session.Image.Preview,
            Kind.Glyph => session.Font.Table.GetGlyph(0x41),
            Kind.Sheet => session.Font.Table.GetGlyph(0),
            _ => null,
        };
        if (bitmap is null)
        {
            return -1;
        }

        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap[x, y])
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static IReadOnlyList<string> Errors(Session session, Kind kind)
    {
        INotifyDataErrorInfo source = kind == Kind.Image ? session.Image : session.Font;
        string name = kind switch
        {
            Kind.Image => nameof(ImageConverterViewModel.ThresholdText),
            Kind.Glyph => nameof(FontGeneratorViewModel.GlyphThresholdText),
            Kind.Sheet => nameof(FontGeneratorViewModel.SheetThresholdText),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        return source.GetErrors(name).Cast<object>().Select(error => error.ToString() ?? string.Empty).ToArray();
    }

    private Session Create()
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        var dialogs = new FakeDialogs();
        var image = new ImageConverterViewModel(
            new MapText(Text),
            dialogs,
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            PresetCatalog.Shared,
            settings.UserPresets,
            ImageTabParameters.CreateDefault());
        var font = new FontGeneratorViewModel(
            new MapText(Text),
            dialogs,
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            new StubOutlines(),
            PresetCatalog.Shared,
            settings.UserPresets,
            settings.Font);
        font.Family = "Stub";
        return new Session(image, font, dialogs);
    }

    private sealed class Session : IDisposable
    {
        public Session(ImageConverterViewModel image, FontGeneratorViewModel font, FakeDialogs dialogs)
        {
            Image = image;
            Font = font;
            Dialogs = dialogs;
        }

        public ImageConverterViewModel Image { get; }

        public FontGeneratorViewModel Font { get; }

        public FakeDialogs Dialogs { get; }

        public void Dispose()
        {
        }
    }

    private sealed class TextHolder
    {
        public string Text { get; set; } = string.Empty;
    }
}

