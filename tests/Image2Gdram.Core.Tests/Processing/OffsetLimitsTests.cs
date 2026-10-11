using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Tests.Processing;

/// <summary>N-67: модуль смещения не больше 80 % стороны, округление вниз.</summary>
public class OffsetLimitsTests
{
    public static TheoryData<int, int> Sides => new()
    {
        { 1, 0 },
        { 2, 1 },
        { 5, 4 },
        { 8, 6 },
        { 12, 9 },
        { 16, 12 },
        { 64, 51 },
        { 128, 102 },
        { 240, 192 },
        { 1024, 819 },
    };

    [Fact]
    public void Percent_is_80() => Assert.Equal(80, OffsetLimits.Percent);

    [Theory]
    [MemberData(nameof(Sides))]
    public void Percent_of_side_rounds_down(int side, int expected) =>
        Assert.Equal(expected, OffsetLimits.For(side));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Side_below_one_is_rejected(int side) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => OffsetLimits.For(side));

    [Theory]
    [InlineData(100, 64, 1024, 51)]
    [InlineData(-100, 64, 1024, -51)]
    [InlineData(51, 64, 1024, 51)]
    [InlineData(0, 64, 1024, 0)]
    [InlineData(5, 1, 1024, 0)]
    [InlineData(-5, 1, 1024, 0)]
    [InlineData(80, 128, 50, 50)]
    [InlineData(-80, 128, 50, -50)]
    [InlineData(500, 128, 1024, 102)]
    [InlineData(5000, 128, 1024, 102)]
    public void Clamp_keeps_the_sign_and_respects_the_absolute_maximum(int offset, int side, int absoluteMax, int expected) =>
        Assert.Equal(expected, OffsetLimits.Clamp(offset, side, absoluteMax));
}
