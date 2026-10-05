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
            Fit = FitMode.Stretch,
            TargetWidth = 4,
            TargetHeight = 4,
            Resample = ResampleMode.NearestNeighbor,
            Binarize = BinarizeMode.Threshold,
            Threshold = 128,
            Alignment = Alignment.TopLeft,
        };

        RgbaImage expectedImage = ResizeStep.Apply(
            RotateFlipStep.Apply(BackgroundCompositor.Apply(source, BackgroundColor.White), Rotation.Rotate90, false, false),
            4,
            4,
            FitMode.Stretch,
            Alignment.TopLeft,
            0,
            0,
            ResampleMode.NearestNeighbor,
            BackgroundColor.White);
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
            SizeMode = TargetSizeMode.MatchSource,
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
            SizeMode = TargetSizeMode.MatchSource,
            Threshold = 128,
        };

        MonoBitmap bitmap = _pipeline.Run(source, options);
        Assert.False(bitmap[0, 0]);
    }

    [Fact]
    public void Match_source_rejects_a_side_above_1024_and_accepts_1024()
    {
        var tooWide = ProcessingTestImages.Solid(1025, 2, 0, 0, 0);
        var rotated = ProcessingTestImages.Solid(2, 1025, 0, 0, 0);
        var options = new ProcessingOptions { SizeMode = TargetSizeMode.MatchSource };

        PipelineException wide = Assert.Throws<PipelineException>(() => _pipeline.Run(tooWide, options));
        Assert.Equal(PipelineErrorCode.SourceLargerThan1024, wide.Code);
        Assert.Equal(1025, wide.Width);

        PipelineException tall = Assert.Throws<PipelineException>(() => _pipeline.Run(rotated, options with { Rotation = Rotation.Rotate90 }));
        Assert.Equal(1025, tall.Width);

        MonoBitmap accepted = _pipeline.Run(ProcessingTestImages.Solid(1024, 2, 0, 0, 0), options);
        Assert.Equal(1024, accepted.Width);
    }

    [Fact]
    public void Pack_is_the_seventh_step_and_inversion_flips_the_byte()
    {
        var source = new RgbaImage(8, 8, 255, 255, 255, 255);
        source.SetPixel(0, 0, 0, 0, 0, 255);
        var options = new ProcessingOptions { SizeMode = TargetSizeMode.MatchSource, Threshold = 128 };
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
            Fit = FitMode.Stretch,
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
        var options = new ProcessingOptions { TargetWidth = 8, TargetHeight = 8, Fit = FitMode.Stretch, Threshold = 1 };
        MonoBitmap dark = _pipeline.Run(source, options);
        MonoBitmap bright = _pipeline.Run(source, options with { Threshold = 255 });

        Assert.Equal(0, ProcessingTestImages.CountActive(dark));
        Assert.Equal(64, ProcessingTestImages.CountActive(bright));

        MonoBitmap smaller = _pipeline.Run(source, options with { TargetWidth = 4, TargetHeight = 4, Threshold = 255 });
        Assert.Equal(4, smaller.Width);
        Assert.Equal(16, ProcessingTestImages.CountActive(smaller));
    }

    [Fact]
    public void Invalid_manual_size_and_threshold_are_rejected()
    {
        RgbaImage source = ProcessingTestImages.Solid(2, 2, 0, 0, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.Run(source, new ProcessingOptions { TargetWidth = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.Run(source, new ProcessingOptions { TargetHeight = 1025 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.Run(source, new ProcessingOptions { Threshold = 256 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.Run(source, new ProcessingOptions { OffsetX = 1025 }));
    }
}
