using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Tests.Processing;

public class BackgroundCompositorTests
{
    [Theory]
    [InlineData(0, 0, 255, 255)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(200, 255, 0, 200)]
    [InlineData(200, 255, 255, 200)]
    [InlineData(255, 128, 0, 128)]
    [InlineData(0, 128, 255, 127)]
    [InlineData(200, 128, 255, 227)]
    public void Channel_mixes_by_alpha(int color, int alpha, int background, int expected)
    {
        Assert.Equal(expected, BackgroundCompositor.Channel((byte)color, (byte)alpha, (byte)background));
    }

    [Fact]
    public void Opaque_pixel_keeps_color_and_transparent_pixel_becomes_background()
    {
        var source = new RgbaImage(2, 1);
        source.SetPixel(0, 0, 10, 20, 30, 255);
        source.SetPixel(1, 0, 10, 20, 30, 0);

        RgbaImage white = BackgroundCompositor.Apply(source, BackgroundColor.White);
        white.GetPixel(0, 0, out byte r, out byte g, out byte b, out byte a);
        Assert.Equal((10, 20, 30, 255), (r, g, b, a));
        white.GetPixel(1, 0, out r, out g, out b, out a);
        Assert.Equal((255, 255, 255, 255), (r, g, b, a));

        RgbaImage black = BackgroundCompositor.Apply(source, BackgroundColor.Black);
        black.GetPixel(1, 0, out r, out g, out b, out a);
        Assert.Equal((0, 0, 0, 255), (r, g, b, a));
    }
}
