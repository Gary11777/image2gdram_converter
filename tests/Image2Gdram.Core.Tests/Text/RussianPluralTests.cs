using Image2Gdram.Core.Text;

namespace Image2Gdram.Core.Tests.Text;

/// <summary>Решение D-04: «1 байт», «1024 байта», «1536 байт».</summary>
public class RussianPluralTests
{
    [Theory]
    [InlineData(0, "0 байт")]
    [InlineData(1, "1 байт")]
    [InlineData(2, "2 байта")]
    [InlineData(4, "4 байта")]
    [InlineData(5, "5 байт")]
    [InlineData(6, "6 байт")]
    [InlineData(11, "11 байт")]
    [InlineData(12, "12 байт")]
    [InlineData(14, "14 байт")]
    [InlineData(21, "21 байт")]
    [InlineData(22, "22 байта")]
    [InlineData(24, "24 байта")]
    [InlineData(25, "25 байт")]
    [InlineData(101, "101 байт")]
    [InlineData(111, "111 байт")]
    [InlineData(112, "112 байт")]
    [InlineData(1024, "1024 байта")]
    [InlineData(1536, "1536 байт")]
    [InlineData(3840, "3840 байт")]
    [InlineData(6144, "6144 байта")]
    [InlineData(131072, "131072 байта")]
    public void Bytes_follow_russian_plural_rule(long count, string expected)
    {
        Assert.Equal(expected, RussianPlural.Bytes(count));
    }

    [Theory]
    [InlineData(1, "1 символ")]
    [InlineData(3, "3 символа")]
    [InlineData(256, "256 символов")]
    public void Symbols(long count, string expected)
    {
        Assert.Equal(expected, RussianPlural.Symbols(count));
    }

    [Theory]
    [InlineData(1, "1 кадр")]
    [InlineData(3, "3 кадра")]
    [InlineData(12, "12 кадров")]
    [InlineData(21, "21 кадр")]
    public void Frames(long count, string expected)
    {
        Assert.Equal(expected, RussianPlural.Frames(count));
    }
}
