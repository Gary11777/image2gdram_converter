using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Tests.Processing;

/// <summary>Шаг 3 читает фрагмент через <see cref="CropGeometry"/> (N-80).</summary>
public class ResizeStepTests
{
    [Fact]
    public void Scale_one_centered_on_the_same_size_copies_the_source()
    {
        var source = new RgbaImage(5, 3);
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                source.SetPixel(x, y, (byte)(10 * x), (byte)(20 * y), 7, 255);
            }
        }

        RgbaImage nearest = Render(source, 5, 3, CropGeometry.Fit(5, 3, 5, 3, Rotation.Rotate0), ResampleMode.NearestNeighbor, BackgroundColor.White);
        RgbaImage area = Render(source, 5, 3, CropGeometry.Fit(5, 3, 5, 3, Rotation.Rotate0), ResampleMode.AreaAverage, BackgroundColor.White);
        Assert.True(source.ContentEquals(nearest));
        Assert.True(source.ContentEquals(area));
    }

    [Fact]
    public void Exact_half_scale_matches_the_former_fit_center()
    {
        var source = new RgbaImage(8, 4);
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                source.SetPixel(x, y, (byte)(x * 20), (byte)(y * 40), 1, 255);
            }
        }

        CropViewport fit = CropGeometry.Fit(8, 4, 4, 2, Rotation.Rotate0);
        Assert.Equal(0.5, fit.Scale);
        RgbaImage result = Render(source, 4, 2, fit, ResampleMode.AreaAverage, BackgroundColor.White);
        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                result.GetPixel(x, y, out byte r, out byte g, out _, out _);
                int expectedR = (sourcePixel(x * 2, y * 2) + sourcePixel(x * 2 + 1, y * 2)
                    + sourcePixel(x * 2, y * 2 + 1) + sourcePixel(x * 2 + 1, y * 2 + 1)) / 4;
                Assert.Equal(expectedR, r);
                Assert.Equal((byte)((y * 2 * 40 + (y * 2 + 1) * 40) / 2), g);
            }
        }

        byte sourcePixel(int x, int y)
        {
            source.GetPixel(x, y, out byte r, out _, out _, out _);
            return r;
        }
    }

    [Fact]
    public void Exact_double_nearest_duplicates_each_pixel()
    {
        var source = new RgbaImage(3, 2);
        source.SetPixel(0, 0, 10, 0, 0, 255);
        source.SetPixel(1, 0, 20, 0, 0, 255);
        source.SetPixel(2, 0, 30, 0, 0, 255);
        source.SetPixel(0, 1, 40, 0, 0, 255);
        source.SetPixel(1, 1, 50, 0, 0, 255);
        source.SetPixel(2, 1, 60, 0, 0, 255);
        CropViewport fit = CropGeometry.Fit(3, 2, 6, 4, Rotation.Rotate0);
        Assert.Equal(2, fit.Scale);
        RgbaImage result = Render(source, 6, 4, fit, ResampleMode.NearestNeighbor, BackgroundColor.Black);
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                result.GetPixel(x, y, out byte r, out _, out _, out _);
                source.GetPixel(x / 2, y / 2, out byte expected, out _, out _, out _);
                Assert.Equal(expected, r);
            }
        }
    }

    [Fact]
    public void Letterbox_240_from_128_keeps_integer_centering()
    {
        var source = new RgbaImage(128, 64, 200, 0, 0, 255);
        CropViewport fit = CropGeometry.Fit(128, 64, 240, 128, Rotation.Rotate0);
        Assert.Equal(1.875, fit.Scale);
        RgbaImage result = Render(source, 240, 128, fit, ResampleMode.NearestNeighbor, BackgroundColor.White);
        for (int y = 0; y < 4; y++)
        {
            result.GetPixel(0, y, out byte r, out _, out _, out _);
            Assert.Equal(255, r);
            result.GetPixel(0, 124 + y, out r, out _, out _, out _);
            Assert.Equal(255, r);
        }

        result.GetPixel(0, 4, out byte content, out _, out _, out _);
        Assert.Equal(200, content);
    }

    [Theory]
    [InlineData(BackgroundColor.White, 255)]
    [InlineData(BackgroundColor.Black, 0)]
    public void Area_outside_the_image_is_the_step1_background(BackgroundColor background, byte expected)
    {
        var source = new RgbaImage(4, 4, 10, 20, 30, 255);
        var shifted = new CropViewport(0, 2, 1);
        RgbaImage result = Render(source, 4, 4, shifted, ResampleMode.AreaAverage, background);
        result.GetPixel(0, 2, out byte r, out byte g, out byte b, out byte a);
        Assert.Equal((expected, expected, expected, (byte)255), (r, g, b, a));
        result.GetPixel(3, 2, out r, out g, out b, out _);
        Assert.Equal(10, r);
        Assert.Equal(20, g);
        Assert.Equal(30, b);
    }

    [Fact]
    public void Fractional_center_is_deterministic()
    {
        var source = new RgbaImage(6, 4);
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                source.SetPixel(x, y, (byte)(x * 30), (byte)(y * 40), 8, 255);
            }
        }

        var crop = new CropViewport(2.5, 1.5, 1.25);
        RgbaImage first = Render(source, 5, 3, crop, ResampleMode.AreaAverage, BackgroundColor.White);
        RgbaImage second = Render(source, 5, 3, crop, ResampleMode.AreaAverage, BackgroundColor.White);
        Assert.True(first.ContentEquals(second));
        first.GetPixel(0, 0, out byte r, out _, out _, out _);
        Assert.InRange(r, 0, 255);
    }

    [Fact]
    public void Nearest_at_scale_two_duplicates_a_two_by_two_block()
    {
        var source = new RgbaImage(2, 2);
        source.SetPixel(0, 0, 1, 0, 0, 255);
        source.SetPixel(1, 0, 2, 0, 0, 255);
        source.SetPixel(0, 1, 3, 0, 0, 255);
        source.SetPixel(1, 1, 4, 0, 0, 255);
        var crop = new CropViewport(1, 1, 2);
        RgbaImage result = Render(source, 4, 4, crop, ResampleMode.NearestNeighbor, BackgroundColor.White);
        AssertPixel(result, 0, 0, 1);
        AssertPixel(result, 1, 0, 1);
        AssertPixel(result, 0, 1, 1);
        AssertPixel(result, 2, 0, 2);
        AssertPixel(result, 0, 2, 3);
        AssertPixel(result, 3, 3, 4);
    }

    [Fact]
    public void Area_at_half_scale_averages_a_two_by_two_block()
    {
        var source = new RgbaImage(2, 2);
        source.SetPixel(0, 0, 0, 0, 0, 255);
        source.SetPixel(1, 0, 100, 0, 0, 255);
        source.SetPixel(0, 1, 0, 0, 0, 255);
        source.SetPixel(1, 1, 0, 0, 0, 255);
        var crop = new CropViewport(1, 1, 0.5);
        RgbaImage result = Render(source, 1, 1, crop, ResampleMode.AreaAverage, BackgroundColor.White);
        result.GetPixel(0, 0, out byte r, out _, out _, out _);
        Assert.Equal(25, r);
    }

    private static void AssertPixel(RgbaImage image, int x, int y, byte r)
    {
        image.GetPixel(x, y, out byte actual, out _, out _, out _);
        Assert.Equal(r, actual);
    }

    private static RgbaImage Render(RgbaImage source, int width, int height, CropViewport crop, ResampleMode resample, BackgroundColor background)
    {
        CropWindow window = CropGeometry.Window(crop, source.Width, source.Height, width, height, Rotation.Rotate0, false, false);
        return ResizeStep.Apply(source, width, height, window.OriginX, window.OriginY, window.Span, resample, background);
    }
}
