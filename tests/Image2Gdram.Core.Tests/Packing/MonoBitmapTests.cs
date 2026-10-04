using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Tests.Packing;

public class MonoBitmapTests
{
    [Fact]
    public void New_bitmap_is_all_background()
    {
        var bitmap = new MonoBitmap(3, 2);

        Assert.Equal(3, bitmap.Width);
        Assert.Equal(2, bitmap.Height);
        Assert.All(Enumerable.Range(0, 2), y => Assert.DoesNotContain(true, bitmap.GetRow(y).ToArray()));
    }

    [Fact]
    public void Row_reflects_pixels_left_to_right()
    {
        MonoBitmap bitmap = TestBitmaps.FromRows("#..", ".#.");

        Assert.Equal(new[] { true, false, false }, bitmap.GetRow(0).ToArray());
        Assert.Equal(new[] { false, true, false }, bitmap.GetRow(1).ToArray());
    }

    [Fact]
    public void Clone_is_independent_and_equal()
    {
        MonoBitmap original = TestBitmaps.FromRows("#.", ".#");
        MonoBitmap clone = original.Clone();

        Assert.True(original.ContentEquals(clone));
        clone[0, 0] = false;
        Assert.False(original.ContentEquals(clone));
        Assert.True(original[0, 0]);
    }

    [Fact]
    public void Content_equality_requires_same_size()
    {
        Assert.False(new MonoBitmap(2, 3).ContentEquals(new MonoBitmap(3, 2)));
        Assert.False(new MonoBitmap(2, 3).ContentEquals(null));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 2)]
    public void Out_of_range_access_throws(int x, int y)
    {
        var bitmap = new MonoBitmap(3, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap[x, y]);
    }
}
