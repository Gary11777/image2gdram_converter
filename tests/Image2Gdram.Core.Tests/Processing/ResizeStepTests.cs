using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Tests.Processing;

public class ResizeStepTests
{
    [Theory]
    [InlineData(FitMode.None, 100, 50)]
    [InlineData(FitMode.Stretch, 30, 40)]
    [InlineData(FitMode.Fit, 30, 15)]
    [InlineData(FitMode.Fill, 80, 40)]
    public void Scaled_size_follows_fit_mode(FitMode fit, int width, int height)
    {
        ResizeStep.ScaledSize size = ResizeStep.GetScaledSize(100, 50, 30, 40, fit);
        Assert.Equal(new ResizeStep.ScaledSize(width, height), size);
    }

    [Fact]
    public void Undefined_side_rounds_half_up_and_stays_at_least_one()
    {
        Assert.Equal(11, ResizeStep.RoundHalfUp(1050, 100));
        Assert.Equal(10, ResizeStep.RoundHalfUp(1010, 100));
        Assert.Equal(new ResizeStep.ScaledSize(10, 11), ResizeStep.GetScaledSize(100, 105, 10, 100, FitMode.Fit));
        Assert.Equal(new ResizeStep.ScaledSize(1, 1), ResizeStep.GetScaledSize(1000, 1, 1, 1, FitMode.Fit));
    }

    [Fact]
    public void Fit_centers_with_odd_remainder_toward_top()
    {
        RgbaImage source = ProcessingTestImages.Solid(100, 50, 255, 0, 0);
        RgbaImage result = Resize(source, 30, 40, FitMode.Fit, Alignment.Center, BackgroundColor.Black);

        Assert.Equal(new ResizeStep.ScaledSize(30, 15), ResizeStep.GetScaledSize(100, 50, 30, 40, FitMode.Fit));
        AssertBackgroundRow(result, 11, background: true);
        AssertBackgroundRow(result, 12, background: false);
        AssertBackgroundRow(result, 26, background: false);
        AssertBackgroundRow(result, 27, background: true);
    }

    [Theory]
    [InlineData(Alignment.TopLeft, 0, 0)]
    [InlineData(Alignment.TopCenter, 2, 0)]
    [InlineData(Alignment.TopRight, 4, 0)]
    [InlineData(Alignment.MiddleLeft, 0, 1)]
    [InlineData(Alignment.Center, 2, 1)]
    [InlineData(Alignment.MiddleRight, 4, 1)]
    [InlineData(Alignment.BottomLeft, 0, 2)]
    [InlineData(Alignment.BottomCenter, 2, 2)]
    [InlineData(Alignment.BottomRight, 4, 2)]
    public void Nine_alignments_place_unscaled_image(Alignment alignment, int originX, int originY)
    {
        Assert.Equal((originX, originY), ResizeStep.GetOrigin(alignment, 6, 4, 2, 2));
        RgbaImage source = ProcessingTestImages.Solid(2, 2, 200, 0, 0);
        RgbaImage result = Resize(source, 6, 4, FitMode.None, alignment, BackgroundColor.White);

        result.GetPixel(originX, originY, out byte r, out _, out _, out byte a);
        Assert.Equal((200, 255), (r, a));
        result.GetPixel(originX == 0 ? 5 : 0, originY == 0 ? 3 : 0, out r, out byte g, out byte b, out a);
        Assert.Equal((255, 255, 255, 255), (r, g, b, a));
    }

    [Fact]
    public void Odd_center_remainder_goes_right_and_down()
    {
        Assert.Equal((1, 1), ResizeStep.GetOrigin(Alignment.Center, 5, 5, 2, 2));
        RgbaImage source = ProcessingTestImages.Solid(2, 2, 9, 0, 0);
        RgbaImage result = Resize(source, 5, 5, FitMode.None, Alignment.Center, BackgroundColor.White);

        result.GetPixel(1, 1, out byte r, out _, out _, out _);
        Assert.Equal(9, r);
        result.GetPixel(0, 1, out r, out _, out _, out _);
        Assert.Equal(255, r);
        result.GetPixel(4, 1, out r, out _, out _, out _);
        Assert.Equal(255, r);
    }

    [Fact]
    public void Offset_shifts_and_clips()
    {
        RgbaImage source = ProcessingTestImages.Solid(2, 2, 7, 0, 0);
        RgbaImage shifted = Resize(source, 4, 4, FitMode.None, Alignment.TopLeft, BackgroundColor.White, offsetX: 1, offsetY: -1);

        shifted.GetPixel(1, 0, out byte r, out _, out _, out _);
        Assert.Equal(7, r);
        shifted.GetPixel(0, 0, out r, out _, out _, out _);
        Assert.Equal(255, r);
    }

    [Fact]
    public void Uncovered_pixels_use_step1_background()
    {
        RgbaImage source = ProcessingTestImages.Solid(1, 1, 255, 0, 0);
        RgbaImage result = Resize(source, 3, 3, FitMode.None, Alignment.TopLeft, BackgroundColor.Black);

        result.GetPixel(0, 0, out byte r, out byte g, out byte b, out byte a);
        Assert.Equal((255, 0, 0, 255), (r, g, b, a));
        result.GetPixel(2, 2, out r, out g, out b, out a);
        Assert.Equal((0, 0, 0, 255), (r, g, b, a));
    }

    [Fact]
    public void None_crops_and_fill_crops_the_long_side()
    {
        RgbaImage source = new(4, 2);
        for (int x = 0; x < 4; x++)
        {
            source.SetPixel(x, 0, (byte)(x + 1), 0, 0, 255);
            source.SetPixel(x, 1, (byte)(x + 1), 0, 0, 255);
        }

        RgbaImage cropped = Resize(source, 2, 2, FitMode.None, Alignment.TopLeft, BackgroundColor.White);
        cropped.GetPixel(0, 0, out byte left, out _, out _, out _);
        cropped.GetPixel(1, 0, out byte next, out _, out _, out _);
        Assert.Equal((1, 2), (left, next));

        RgbaImage filled = Resize(source, 2, 2, FitMode.Fill, Alignment.Center, BackgroundColor.White, ResampleMode.NearestNeighbor);
        filled.GetPixel(0, 0, out byte first, out _, out _, out _);
        filled.GetPixel(1, 0, out byte second, out _, out _, out _);
        Assert.Equal((2, 3), (first, second));
    }

    [Fact]
    public void Stretch_changes_both_axes()
    {
        RgbaImage source = ProcessingTestImages.Solid(2, 2, 4, 5, 6);
        RgbaImage result = Resize(source, 5, 3, FitMode.Stretch, Alignment.TopLeft, BackgroundColor.White);

        Assert.Equal(5, result.Width);
        Assert.Equal(3, result.Height);
        result.GetPixel(4, 2, out byte r, out byte g, out byte b, out _);
        Assert.Equal((4, 5, 6), (r, g, b));
    }

    [Fact]
    public void Nearest_neighbor_doubles_pixel_art_without_mixing()
    {
        var source = new RgbaImage(2, 2);
        source.SetPixel(0, 0, 10, 0, 0, 255);
        source.SetPixel(1, 0, 20, 0, 0, 255);
        source.SetPixel(0, 1, 30, 0, 0, 255);
        source.SetPixel(1, 1, 40, 0, 0, 255);

        RgbaImage result = ResizeStep.Resample(source, 4, 4, ResampleMode.NearestNeighbor);

        Assert.Equal((10, 10, 20, 20), Row(result, 0));
        Assert.Equal((10, 10, 20, 20), Row(result, 1));
        Assert.Equal((30, 30, 40, 40), Row(result, 2));
        Assert.Equal((30, 30, 40, 40), Row(result, 3));
    }

    [Fact]
    public void Area_average_keeps_a_thin_line_that_nearest_neighbor_skips()
    {
        var source = new RgbaImage(8, 4, 255, 255, 255, 255);
        for (int y = 0; y < 4; y++)
        {
            source.SetPixel(0, y, 0, 0, 0, 255);
        }

        RgbaImage nearest = ResizeStep.Resample(source, 4, 4, ResampleMode.NearestNeighbor);
        nearest.GetPixel(0, 0, out byte nearestRed, out _, out _, out _);
        Assert.Equal(255, nearestRed);

        RgbaImage average = ResizeStep.Resample(source, 4, 4, ResampleMode.AreaAverage);
        average.GetPixel(0, 0, out byte averaged, out _, out _, out _);
        average.GetPixel(1, 0, out byte untouched, out _, out _, out _);
        Assert.Equal(128, averaged);
        Assert.Equal(255, untouched);
    }

    [Fact]
    public void Area_average_of_equal_quarters_rounds_half_away_from_zero()
    {
        var source = new RgbaImage(2, 2);
        source.SetPixel(0, 0, 0, 0, 0, 255);
        source.SetPixel(1, 0, 255, 255, 255, 255);
        source.SetPixel(0, 1, 255, 255, 255, 255);
        source.SetPixel(1, 1, 0, 0, 0, 255);

        RgbaImage result = ResizeStep.Resample(source, 1, 1, ResampleMode.AreaAverage);
        result.GetPixel(0, 0, out byte r, out _, out _, out _);
        Assert.Equal(128, r);
    }

    [Fact]
    public void Resample_is_repeatable()
    {
        var source = new RgbaImage(5, 3);
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                source.SetPixel(x, y, (byte)(x * 40 + y), (byte)(255 - x * 20), 10, 255);
            }
        }

        RgbaImage first = ResizeStep.Resample(source, 3, 2, ResampleMode.AreaAverage);
        RgbaImage second = ResizeStep.Resample(source, 3, 2, ResampleMode.AreaAverage);
        Assert.True(first.ContentEquals(second));
    }

    private static RgbaImage Resize(
        RgbaImage source,
        int width,
        int height,
        FitMode fit,
        Alignment alignment,
        BackgroundColor background,
        ResampleMode resample = ResampleMode.NearestNeighbor,
        int offsetX = 0,
        int offsetY = 0) =>
        ResizeStep.Apply(source, width, height, fit, alignment, offsetX, offsetY, resample, background);

    private static void AssertBackgroundRow(RgbaImage image, int y, bool background)
    {
        image.GetPixel(0, y, out byte r, out _, out _, out _);
        Assert.Equal(background ? 0 : 255, r);
    }

    private static (byte, byte, byte, byte) Row(RgbaImage image, int y)
    {
        image.GetPixel(0, y, out byte a, out _, out _, out _);
        image.GetPixel(1, y, out byte b, out _, out _, out _);
        image.GetPixel(2, y, out byte c, out _, out _, out _);
        image.GetPixel(3, y, out byte d, out _, out _, out _);
        return (a, b, c, d);
    }
}
