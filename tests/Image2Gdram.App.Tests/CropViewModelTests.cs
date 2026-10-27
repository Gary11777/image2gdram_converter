using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Settings;
using image2gdram_converter;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.App.Tests;

/// <summary>Прямоугольник фрагмента на вкладке картинок (N-80).</summary>
public class CropViewModelTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-crop-" + Guid.NewGuid().ToString("N"));

    public CropViewModelTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void Loading_an_image_fits_the_rectangle()
    {
        ImageConverterViewModel vm = Create();
        vm.WidthText = "128";
        vm.HeightText = "64";
        Load(vm, 240, 128, userAction: true);

        CropViewport fit = CropGeometry.Fit(240, 128, 128, 64, Rotation.Rotate0);
        Assert.Equal(fit, vm.Crop);
        Assert.Null(vm.ToParameters().Processing.Crop);
        Assert.Equal(ScaleFormat.Format(fit.Scale), vm.ScaleText);
    }

    [Fact]
    public void Rotation_refits_and_swaps_the_rectangle_sides()
    {
        ImageConverterViewModel vm = Create();
        vm.WidthText = "128";
        vm.HeightText = "64";
        Load(vm, 240, 128, userAction: true);
        SourceRect before = vm.CropRect;

        vm.Rotation = Rotation.Rotate90;

        CropViewport fit = CropGeometry.Fit(240, 128, 128, 64, Rotation.Rotate90);
        Assert.Equal(fit, vm.Crop);
        SourceRect after = vm.CropRect;
        Assert.NotEqual(before.Width, after.Width);
        Assert.Equal(fit.Scale, vm.Crop.Scale);
    }

    [Fact]
    public void Flip_does_not_change_the_crop()
    {
        ImageConverterViewModel vm = Create();
        Load(vm, 32, 16, userAction: true);
        vm.Crop = new CropViewport(10, 8, 1.5);
        CropViewport stored = vm.ToParameters().Processing.Crop!.Value;

        vm.FlipHorizontal = true;
        vm.FlipVertical = true;

        Assert.Equal(stored, vm.ToParameters().Processing.Crop);
    }

    [Fact]
    public void Preset_fits_the_new_size()
    {
        ImageConverterViewModel vm = Create();
        Load(vm, 240, 128, userAction: true);
        vm.Crop = new CropViewport(20, 20, 2);
        PresetChoice item = vm.Presets.Items.First(entry => entry.Preset?.Name == "OLED128X64-0.96");

        vm.Presets.Selected = item;

        Assert.Equal(128, vm.TargetWidth);
        Assert.Equal(64, vm.TargetHeight);
        Assert.Null(vm.ToParameters().Processing.Crop);
        Assert.Equal(CropGeometry.Fit(240, 128, 128, 64, Rotation.Rotate0), vm.Crop);
    }

    [Fact]
    public void Scale_outside_the_range_blocks_save_and_copy()
    {
        ImageConverterViewModel vm = Create();
        Load(vm, 32, 16, userAction: true);
        Assert.True(vm.CanCopy);

        vm.ScaleText = "9999";

        Assert.False(vm.CanCopy);
        Assert.False(vm.CanSaveOutput);
        Assert.NotEmpty(vm.GetErrors(nameof(vm.ScaleText)));
    }

    [Fact]
    public void Crop_gesture_clears_edits_only_after_confirmation()
    {
        var dialogs = new FakeDialogs { ConfirmAnswer = false };
        ImageConverterViewModel vm = Create(dialogs);
        Load(vm, 16, 16, userAction: true);
        vm.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);
        Assert.True(vm.HasEdits);
        CropViewport before = vm.Crop;

        Assert.False(vm.BeginCropGesture());
        Assert.True(vm.HasEdits);
        Assert.Equal(before, vm.Crop);

        dialogs.ConfirmAnswer = true;
        Assert.True(vm.BeginCropGesture());
        Assert.False(vm.HasEdits);
        vm.Crop = new CropViewport(4, 4, 2);
        Assert.Equal(2, vm.ToParameters().Processing.Crop!.Value.Scale);
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

    private void Load(ImageConverterViewModel vm, int width, int height, bool userAction)
    {
        var image = new RgbaImage(width, height, 255, 255, 255, 255);
        vm.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { image }), "pic.png", resetFrame: true, userAction);
    }

    private ImageConverterViewModel Create(FakeDialogs? dialogs = null)
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        return new ImageConverterViewModel(
            new MapText(TestFiles.LoadUiStrings()),
            dialogs ?? new FakeDialogs(),
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            PresetCatalog.Shared,
            settings.UserPresets,
            ImageTabParameters.CreateDefault());
    }
}
