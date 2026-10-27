using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Tests.Processing;

public class ImagePipelineTests
{
    private readonly ImagePipeline _pipeline = new();

    [Fact]
    public void Steps_run_in_order_and_match_the_individual_classes()
    {
        var source = new RgbaImage(4, 2);
        source.SetPixel(0, 0, 255, 0, 0, 128);
        source.SetPixel(3, 1, 0, 0, 0, 255);
        var options = new ProcessingOptions
        {
            Background = BackgroundColor.White,
            Rotation = Rotation.Rotate90,
            TargetWidth = 4,
            TargetHeight = 4,
            Resample = ResampleMode.NearestNeighbor,
            Binarize = BinarizeMode.Threshold,
            Threshold = 128,
        };

        RgbaImage oriented = RotateFlipStep.Apply(BackgroundCompositor.Apply(source, BackgroundColor.White), Rotation.Rotate90, false, false);
        CropViewport crop = CropGeometry.Fit(source.Width, source.Height, 4, 4, Rotation.Rotate90);
        CropWindow window = CropGeometry.Window(crop, source.Width, source.Height, 4, 4, Rotation.Rotate90, false, false);
        RgbaImage expectedImage = ResizeStep.Apply(
            oriented, 4, 4, window.OriginX, window.OriginY, window.Span, ResampleMode.NearestNeighbor, BackgroundColor.White);
        MonoBitmap expected = new ThresholdBinarizer().Apply(GrayscaleStep.Apply(expectedImage), 128);

        MonoBitmap actual = _pipeline.Run(source, options);
        Assert.True(expected.ContentEquals(actual));
        Assert.Equal(4, actual.Width);
        Assert.Equal(4, actual.Height);
    }

    [Fact]
    public void Manual_edits_are_applied_after_binarization()
    {
        RgbaImage source = ProcessingTestImages.Solid(4, 4, 255, 255, 255);
        var options = new ProcessingOptions
        {
            TargetWidth = 4,
            TargetHeight = 4,
            Binarize = BinarizeMode.Threshold,
            Threshold = 128,
        };
        var edits = new PixelOverrides();
        edits.Set(1, 2, true);

        MonoBitmap bitmap = _pipeline.Run(source, options, edits);

        Assert.True(bitmap[1, 2]);
        Assert.Equal(1, ProcessingTestImages.CountActive(bitmap));
    }

    [Fact]
    public void Semi_transparent_red_is_composited_before_the_threshold()
    {
        var source = new RgbaImage(1, 1);
        source.SetPixel(0, 0, 255, 0, 0, 128);
        var options = new ProcessingOptions
        {
            Background = BackgroundColor.White,
            TargetWidth = 1,
            TargetHeight = 1,
            Threshold = 128,
        };

        MonoBitmap bitmap = _pipeline.Run(source, options);
        Assert.False(bitmap[0, 0]);
    }

    [Fact]
    public void Same_size_crop_accepts_1024_and_rejects_a_target_above_1024()
    {
        RgbaImage acceptedSource = ProcessingTestImages.Solid(1024, 2, 0, 0, 0);
        MonoBitmap accepted = _pipeline.Run(acceptedSource, new ProcessingOptions { TargetWidth = 1024, TargetHeight = 2 });
        Assert.Equal(1024, accepted.Width);

        RgbaImage source = ProcessingTestImages.Solid(2, 2, 0, 0, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.Run(source, new ProcessingOptions { TargetWidth = 1025 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.Run(source, new ProcessingOptions { TargetHeight = 0 }));
    }

    [Fact]
    public void Pack_is_the_seventh_step_and_inversion_flips_the_byte()
    {
        var source = new RgbaImage(8, 8, 255, 255, 255, 255);
        source.SetPixel(0, 0, 0, 0, 0, 255);
        var options = new ProcessingOptions { TargetWidth = 8, TargetHeight = 8, Threshold = 128 };
        var packing = new PackingOptions { Direction = PackDirection.Vertical, BitOrder = BitOrder.LsbFirst };

        byte[] bytes = _pipeline.RunAndPack(source, options, packing);
        Assert.Equal(0x01, bytes[0]);

        byte[] inverted = _pipeline.RunAndPack(source, options, packing with { Invert = true });
        Assert.Equal(0xFE, inverted[0]);
    }

    [Fact]
    public void Repeated_run_is_identical_including_dithering()
    {
        var source = new RgbaImage(6, 4);
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                source.SetPixel(x, y, (byte)(x * 30 + y * 10), (byte)(200 - x * 20), 40, (byte)(200 - y * 20));
            }
        }

        var options = new ProcessingOptions
        {
            TargetWidth = 5,
            TargetHeight = 3,
            Crop = new CropViewport(3.25, 2.5, 1.2),
            Binarize = BinarizeMode.FloydSteinberg,
            Threshold = 140,
        };

        MonoBitmap first = _pipeline.Run(source, options);
        MonoBitmap second = _pipeline.Run(source, options);
        Assert.True(first.ContentEquals(second));
    }

    [Fact]
    public void Changing_only_the_threshold_keeps_the_target_size_and_changes_the_pixels()
    {
        RgbaImage source = ProcessingTestImages.Solid(8, 8, 100, 100, 100);
        var options = new ProcessingOptions { TargetWidth = 8, TargetHeight = 8, Threshold = 1 };
        MonoBitmap dark = _pipeline.Run(source, options);
        MonoBitmap bright = _pipeline.Run(source, options with { Threshold = 255 });

        Assert.Equal(0, ProcessingTestImages.CountActive(dark));
        Assert.Equal(64, ProcessingTestImages.CountActive(bright));

        MonoBitmap smaller = _pipeline.Run(source, options with { TargetWidth = 4, TargetHeight = 4, Threshold = 255 });
        Assert.Equal(4, smaller.Width);
        Assert.Equal(16, ProcessingTestImages.CountActive(smaller));
    }

    [Fact]
    public void Moving_the_center_down_shows_the_background_on_top()
    {
        var source = new RgbaImage(4, 16);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                source.SetPixel(x, y, (byte)(y * 16), 0, 0, 255);
            }
        }

        var options = new ProcessingOptions
        {
            TargetWidth = 4,
            TargetHeight = 16,
            Resample = ResampleMode.NearestNeighbor,
            Binarize = BinarizeMode.Threshold,
            Threshold = 128,
        };

        MonoBitmap centered = _pipeline.Run(source, options);
        MonoBitmap shifted = _pipeline.Run(source, options with { Crop = new CropViewport(2, 4, 1) });

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                Assert.Equal(y >= 4 && centered[x, y - 4], shifted[x, y]);
            }
        }
    }

    [Fact]
    public void Invalid_size_threshold_and_scale_are_rejected()
    {
        RgbaImage source = ProcessingTestImages.Solid(2, 2, 0, 0, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.Run(source, new ProcessingOptions { TargetWidth = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.Run(source, new ProcessingOptions { TargetHeight = 1025 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.Run(source, new ProcessingOptions { Threshold = 256 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.Run(source, new ProcessingOptions { Crop = new CropViewport(1, 1, 0) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.Run(source, new ProcessingOptions { Crop = new CropViewport(double.NaN, 1, 1) }));
    }
}
