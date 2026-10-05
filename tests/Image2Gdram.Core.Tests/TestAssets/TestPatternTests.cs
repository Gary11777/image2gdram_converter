using Image2Gdram.TestAssets;

namespace Image2Gdram.Core.Tests.TestAssets;

public class TestPatternTests
{
    [Theory]
    [InlineData(240, 128, "240x128")]
    [InlineData(128, 64, "128x64")]
    public void Screen_has_a_frame_markers_ticks_and_separated_blocks(int width, int height, string label)
    {
        PatternBitmap image = TestPattern.CreateScreen(width, height);

        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
        Assert.Equal(label, image.Label);
        Assert.Equal(PatternElement.Frame, image.OwnerAt(0, 0));
        Assert.True(image.IsActive(0, height - 1));
        Assert.True(image.IsActive(width - 1, 0));
        Assert.Equal(PatternElement.None, image.OwnerAt(1, 1));
        Assert.Equal(PatternElement.None, image.OwnerAt(2, 2));

        AssertMarker(image, TestPattern.MarkerOrigin, TestPattern.MarkerOrigin, PatternElement.MarkerSolid);
        Assert.True(image.IsActive(TestPattern.MarkerOrigin + 3, TestPattern.MarkerOrigin + 3));
        AssertMarker(image, TestPattern.MarkerRight(width), TestPattern.MarkerOrigin, PatternElement.MarkerOutline);
        Assert.False(image.IsActive(TestPattern.MarkerRight(width) + 3, TestPattern.MarkerOrigin + 3));
        AssertMarker(image, TestPattern.MarkerOrigin, TestPattern.MarkerBottom(height), PatternElement.MarkerCross);
        Assert.True(image.IsActive(TestPattern.MarkerOrigin + 3, TestPattern.MarkerBottom(height) + 3));
        Assert.True(image.IsActive(TestPattern.MarkerOrigin, TestPattern.MarkerBottom(height)));
        Assert.False(image.IsActive(TestPattern.MarkerOrigin + 1, TestPattern.MarkerBottom(height)));

        Assert.Equal(PatternElement.Tick, image.OwnerAt(8, 1));
        Assert.Equal(PatternElement.Tick, image.OwnerAt(8, 2));
        Assert.NotEqual(PatternElement.Tick, image.OwnerAt(8, 3));
        Assert.Equal(PatternElement.Tick, image.OwnerAt(64, 5));
        Assert.NotEqual(PatternElement.Tick, image.OwnerAt(64, 6));
        Assert.Equal(PatternElement.Frame, image.OwnerAt(64, 0));

        Assert.Equal(PatternElement.Checker, image.OwnerAt(16, 16));
        Assert.True(image.IsActive(16, 16));
        Assert.False(image.IsActive(17, 16));
        Assert.True(image.IsActive(36, 16));
        Assert.Equal(PatternElement.FilledRectangle, image.OwnerAt(40, 20));
        Assert.Equal(PatternElement.EmptyRectangle, image.OwnerAt(64, 24));
        Assert.False(image.IsActive(64, 24));
        Assert.True(image.IsActive(56, 16));

        Assert.Equal(PatternElement.Label, image.OwnerAt(TestPattern.LabelX, TestPattern.LabelY));
        Assert.False(image.IsActive(TestPattern.LabelX, TestPattern.LabelY));
        int inkColumn = label[0] == '1' ? 2 : 1;
        Assert.True(image.IsActive(TestPattern.LabelX + inkColumn, TestPattern.LabelY));
        Assert.Equal(PatternElement.Diagonal, image.OwnerAt(16, height == 64 ? 36 : 48));
    }

    [Fact]
    public void Sprite_is_13_by_11_and_asymmetric()
    {
        PatternBitmap sprite = TestPattern.CreateSprite();
        Assert.Equal(13, sprite.Width);
        Assert.Equal(11, sprite.Height);
        Assert.True(sprite.IsActive(0, 0));
        Assert.False(sprite.IsActive(3, 0));
        Assert.False(SameWhenMirroredHorizontally(sprite));
        Assert.False(SameWhenMirroredVertically(sprite));
    }

    private static void AssertMarker(PatternBitmap image, int left, int top, PatternElement element)
    {
        Assert.Equal(element, image.OwnerAt(left, top));
        Assert.Equal(element, image.OwnerAt(left + 3, top + 3));
    }

    private static bool SameWhenMirroredHorizontally(PatternBitmap image)
    {
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (image.IsActive(x, y) != image.IsActive(image.Width - 1 - x, y))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool SameWhenMirroredVertically(PatternBitmap image)
    {
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (image.IsActive(x, y) != image.IsActive(x, image.Height - 1 - y))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
