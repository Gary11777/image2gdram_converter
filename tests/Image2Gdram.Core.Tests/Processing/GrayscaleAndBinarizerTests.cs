using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Tests.Processing;

public class GrayscaleAndBinarizerTests
{
    [Theory]
    [InlineData(255, 0, 0, 76)]
    [InlineData(0, 255, 0, 150)]
    [InlineData(0, 0, 255, 29)]
    [InlineData(255, 255, 255, 255)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0)]
    [InlineData(2, 0, 0, 1)]
    public void Luminance_uses_integer_rec601(int r, int g, int b, int expected)
    {
        Assert.Equal(expected, GrayscaleStep.Luminance((byte)r, (byte)g, (byte)b));
    }

    [Fact]
    public void Luminance_ignores_alpha()
    {
        var source = new RgbaImage(1, 1);
        source.SetPixel(0, 0, 0, 0, 255, 10);
        GrayImage gray = GrayscaleStep.Apply(source);
        Assert.Equal(29, gray[0, 0]);
    }

    [Theory]
    [InlineData(127, 128, true)]
    [InlineData(128, 128, false)]
    [InlineData(0, 0, false)]
    [InlineData(254, 255, true)]
    [InlineData(255, 255, false)]
    public void Threshold_is_strictly_less_than(int luminance, int threshold, bool active)
    {
        var gray = new GrayImage(1, 1, new[] { (byte)luminance });
        MonoBitmap bitmap = new ThresholdBinarizer().Apply(gray, threshold);
        Assert.Equal(active, bitmap[0, 0]);
    }

    [Fact]
    public void Floyd_steinberg_matches_hand_calculated_block()
    {
        var gray = new GrayImage(2, 2, new byte[] { 100, 200, 150, 50 });
        MonoBitmap bitmap = new FloydSteinbergDitherer().Apply(gray, 128);
        Assert.Equal(new[] { true, false, false, true }, Pixels(bitmap));
    }

    [Fact]
    public void Atkinson_distributes_only_six_eighths()
    {
        MonoBitmap bitmap = new AtkinsonDitherer().Apply(ProcessingTestImages.Flat(4, 1, 128), 128);
        Assert.Equal(new[] { false, true, true, false }, Pixels(bitmap));
    }

    [Fact]
    public void Floyd_and_atkinson_differ_on_the_same_row()
    {
        GrayImage gray = ProcessingTestImages.Flat(4, 1, 128);
        bool[] floyd = Pixels(new FloydSteinbergDitherer().Apply(gray, 128));
        bool[] atkinson = Pixels(new AtkinsonDitherer().Apply(gray, 128));
        Assert.Equal(new[] { false, true, false, true }, floyd);
        Assert.False(floyd.AsSpan().SequenceEqual(atkinson));
    }

    [Fact]
    public void Dithering_is_repeatable()
    {
        var pixels = new byte[12];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = (byte)(i * 20);
        }

        var gray = new GrayImage(4, 3, pixels);
        Assert.True(new FloydSteinbergDitherer().Apply(gray, 128).ContentEquals(new FloydSteinbergDitherer().Apply(gray, 128)));
        Assert.True(new AtkinsonDitherer().Apply(gray, 90).ContentEquals(new AtkinsonDitherer().Apply(gray, 90)));
    }

    [Fact]
    public void Bayer4_matrix_matches_f10_and_uniform_gray_is_half_active()
    {
        int[,] matrix = BayerDitherer.Matrix(4);
        Assert.Equal(new[] { 0, 8, 2, 10 }, new[] { matrix[0, 0], matrix[0, 1], matrix[0, 2], matrix[0, 3] });
        Assert.Equal(new[] { 12, 4, 14, 6 }, new[] { matrix[1, 0], matrix[1, 1], matrix[1, 2], matrix[1, 3] });
        Assert.Equal(new[] { 3, 11, 1, 9 }, new[] { matrix[2, 0], matrix[2, 1], matrix[2, 2], matrix[2, 3] });
        Assert.Equal(new[] { 15, 7, 13, 5 }, new[] { matrix[3, 0], matrix[3, 1], matrix[3, 2], matrix[3, 3] });

        MonoBitmap bitmap = new BayerDitherer(4).Apply(ProcessingTestImages.Flat(4, 4, 128), 128);
        Assert.Equal(8, ProcessingTestImages.CountActive(bitmap));
        Assert.False(bitmap[0, 0]);
        Assert.True(bitmap[1, 0]);
        Assert.True(bitmap[0, 1]);
        Assert.False(bitmap[1, 1]);
    }

    [Fact]
    public void Bayer8_is_the_recursive_expansion_and_half_of_a_uniform_field()
    {
        int[,] matrix = BayerDitherer.Matrix(8);
        Assert.Equal(0, matrix[0, 0]);
        Assert.Equal(32, matrix[0, 1]);
        Assert.Equal(2, matrix[0, 4]);

        MonoBitmap bitmap = new BayerDitherer(8).Apply(ProcessingTestImages.Flat(16, 16, 128), 128);
        Assert.Equal(128, ProcessingTestImages.CountActive(bitmap));
    }

    [Theory]
    [InlineData(BinarizeMode.FloydSteinberg)]
    [InlineData(BinarizeMode.Atkinson)]
    [InlineData(BinarizeMode.Bayer4)]
    [InlineData(BinarizeMode.Bayer8)]
    public void Higher_threshold_activates_more_pixels(BinarizeMode mode)
    {
        MonoBitmap low = Binarizer.Create(mode).Apply(ProcessingTestImages.Flat(1, 1, 100), 0);
        MonoBitmap high = Binarizer.Create(mode).Apply(ProcessingTestImages.Flat(1, 1, 100), 255);
        Assert.False(low[0, 0]);
        Assert.True(high[0, 0]);

        var ramp = new byte[16];
        for (int i = 0; i < ramp.Length; i++)
        {
            ramp[i] = (byte)(i * 16);
        }

        var gray = new GrayImage(16, 1, ramp);
        int dark = ProcessingTestImages.CountActive(Binarizer.Create(mode).Apply(gray, 32));
        int mid = ProcessingTestImages.CountActive(Binarizer.Create(mode).Apply(gray, 128));
        int bright = ProcessingTestImages.CountActive(Binarizer.Create(mode).Apply(gray, 224));
        Assert.True(dark < mid);
        Assert.True(mid < bright);
    }

    private static bool[] Pixels(MonoBitmap bitmap)
    {
        var values = new bool[bitmap.Width * bitmap.Height];
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                values[y * bitmap.Width + x] = bitmap[x, y];
            }
        }

        return values;
    }
}
