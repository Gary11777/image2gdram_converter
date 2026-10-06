using Image2Gdram.Core.Packing;
using image2gdram_converter;

namespace Image2Gdram.App.Tests;

public class GridAndPaintTests
{
    [Theory]
    [InlineData(100, 64, 128, 64, 1)]
    [InlineData(400, 200, 128, 64, 3)]
    [InlineData(10000, 10000, 10, 10, 32)]
    [InlineData(0, 100, 10, 10, 1)]
    public void Fit_picks_the_largest_integer_scale_that_fits(double viewW, double viewH, int imageW, int imageH, int expected) =>
        Assert.Equal(expected, GridScale.Fit(viewW, viewH, imageW, imageH));

    [Fact]
    public void Thick_step_follows_the_packing_direction()
    {
        Assert.Equal(6, GridScale.ThickStep(PackDirection.Horizontal, 6));
        Assert.Equal(8, GridScale.ThickStep(PackDirection.Horizontal, 8));
        Assert.Equal(8, GridScale.ThickStep(PackDirection.Vertical, 6));
    }

    [Theory]
    [InlineData(false, StrokePaint.Dot, true)]
    [InlineData(true, StrokePaint.Dot, false)]
    [InlineData(false, StrokePaint.Background, false)]
    [InlineData(true, StrokePaint.Background, true)]
    public void Paint_targets_the_visible_dot(bool invert, StrokePaint paint, bool logical) =>
        Assert.Equal(logical, PixelPaint.Logical(invert, paint));

    [Fact]
    public void Displayed_bit_flips_with_inversion()
    {
        Assert.True(PixelPaint.Displayed(logicalActive: true, invert: false));
        Assert.False(PixelPaint.Displayed(logicalActive: true, invert: true));
    }
}
