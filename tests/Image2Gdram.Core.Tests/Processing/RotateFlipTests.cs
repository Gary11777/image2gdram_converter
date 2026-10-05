using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Processing;
using Image2Gdram.TestAssets;

namespace Image2Gdram.Core.Tests.Processing;

public class RotateFlipTests
{
    [Fact]
    public void Rotate90_moves_pixels_clockwise()
    {
        RgbaImage source = Marked(2, 2);
        RgbaImage rotated = RotateFlipStep.Rotate(source, Rotation.Rotate90);

        Assert.Equal(2, rotated.Width);
        Assert.Equal(2, rotated.Height);
        Assert.Equal((3, 1, 4, 2), Channels(rotated));
    }

    [Fact]
    public void Rotate180_and_270_and_flips_follow_n10()
    {
        RgbaImage source = Marked(2, 2);

        Assert.Equal((4, 3, 2, 1), Channels(RotateFlipStep.Rotate(source, Rotation.Rotate180)));
        Assert.Equal((2, 4, 1, 3), Channels(RotateFlipStep.Rotate(source, Rotation.Rotate270)));
        Assert.Equal((2, 1, 4, 3), Channels(RotateFlipStep.Flip(source, horizontal: true, vertical: false)));
        Assert.Equal((3, 4, 1, 2), Channels(RotateFlipStep.Flip(source, horizontal: false, vertical: true)));
    }

    [Fact]
    public void Flip_is_applied_after_rotation()
    {
        RgbaImage source = Marked(2, 2);
        RgbaImage combined = RotateFlipStep.Apply(source, Rotation.Rotate90, flipHorizontal: true, flipVertical: false);
        RgbaImage stepwise = RotateFlipStep.Flip(RotateFlipStep.Rotate(source, Rotation.Rotate90), horizontal: true, vertical: false);

        Assert.True(combined.ContentEquals(stepwise));
        Assert.Equal((1, 3, 2, 4), Channels(combined));
    }

    [Theory]
    [InlineData(240, 128)]
    [InlineData(128, 64)]
    public void Rotation_moves_corner_markers_of_test_pattern(int width, int height)
    {
        PatternBitmap pattern = TestPattern.CreateScreen(width, height);
        RgbaImage source = ProcessingTestImages.FromPattern(pattern);

        RgbaImage clockwise = RotateFlipStep.Rotate(source, Rotation.Rotate90);
        Assert.Equal(height, clockwise.Width);
        Assert.Equal(width, clockwise.Height);
        AssertAllBlack(clockwise, height - TestPattern.MarkerOrigin - TestPattern.MarkerSize, TestPattern.MarkerOrigin);

        RgbaImage mirrored = RotateFlipStep.Flip(source, horizontal: true, vertical: false);
        AssertAllBlack(mirrored, TestPattern.MarkerRight(width), TestPattern.MarkerOrigin);

        RgbaImage flippedDown = RotateFlipStep.Flip(source, horizontal: false, vertical: true);
        AssertAllBlack(flippedDown, TestPattern.MarkerOrigin, TestPattern.MarkerBottom(height));
    }

    private static void AssertAllBlack(RgbaImage image, int left, int top)
    {
        for (int y = top; y < top + TestPattern.MarkerSize; y++)
        {
            for (int x = left; x < left + TestPattern.MarkerSize; x++)
            {
                image.GetPixel(x, y, out byte r, out byte g, out byte b, out byte a);
                Assert.Equal((0, 0, 0, 255), (r, g, b, a));
            }
        }
    }

    private static RgbaImage Marked(int width, int height)
    {
        var image = new RgbaImage(width, height);
        image.SetPixel(0, 0, 1, 0, 0, 255);
        image.SetPixel(1, 0, 2, 0, 0, 255);
        image.SetPixel(0, 1, 3, 0, 0, 255);
        image.SetPixel(1, 1, 4, 0, 0, 255);
        return image;
    }

    /// <summary>Красный канал в порядке (0,0), (1,0), (0,1), (1,1).</summary>
    private static (byte A, byte B, byte C, byte D) Channels(RgbaImage image)
    {
        image.GetPixel(0, 0, out byte a, out _, out _, out _);
        image.GetPixel(1, 0, out byte b, out _, out _, out _);
        image.GetPixel(0, 1, out byte c, out _, out _, out _);
        image.GetPixel(1, 1, out byte d, out _, out _, out _);
        return (a, b, c, d);
    }
}
