using Image2Gdram.Core.Text;

namespace Image2Gdram.Core.Tests.Text;

/// <summary>Таблица CP1251 и печатаемые коды (решения D-10, D-16).</summary>
public class Cp1251Tests
{
    [Theory]
    [InlineData(0x41, 'A')]
    [InlineData(0xC0, 'А')]
    [InlineData(0xDF, 'Я')]
    [InlineData(0xE0, 'а')]
    [InlineData(0xFF, 'я')]
    [InlineData(0xA8, 'Ё')]
    [InlineData(0xB8, 'ё')]
    [InlineData(0x88, '€')]
    [InlineData(0xB9, '№')]
    [InlineData(0x99, '™')]
    [InlineData(0xA0, '\u00A0')]
    [InlineData(0x00, '\0')]
    public void Known_codes_map_to_unicode(int code, char expected)
    {
        Assert.Equal(expected, Cp1251.ToUnicode((byte)code));
        Assert.True(Cp1251.TryFromUnicode(expected, out byte back));
        Assert.Equal(code, back);
    }

    [Fact]
    public void Code_0x98_is_unassigned()
    {
        Assert.Null(Cp1251.ToUnicode(0x98));
        Assert.False(Cp1251.TryFromUnicode('\u0098', out _));
    }

    [Fact]
    public void All_assigned_codes_round_trip()
    {
        for (int code = 0; code < 256; code++)
        {
            if (code == Cp1251.Unassigned)
            {
                continue;
            }

            char c = Cp1251.ToUnicode((byte)code) ?? throw new Xunit.Sdk.XunitException($"0x{code:X2} has no character");
            Assert.True(Cp1251.TryFromUnicode(c, out byte back));
            Assert.Equal(code, back);
        }
    }

    [Theory]
    [InlineData(0x00, false)]
    [InlineData(0x1F, false)]
    [InlineData(0x20, true)]
    [InlineData(0x41, true)]
    [InlineData(0x7E, true)]
    [InlineData(0x7F, false)]
    [InlineData(0x80, true)]
    [InlineData(0x98, false)]
    [InlineData(0xA0, false)]
    [InlineData(0xAD, false)]
    [InlineData(0xA8, true)]
    [InlineData(0xC0, true)]
    [InlineData(0xFF, true)]
    public void Printable_codes_follow_d10(int code, bool printable)
    {
        Assert.Equal(printable, Cp1251.IsPrintable((byte)code));
    }

    [Fact]
    public void Get_bytes_encodes_cyrillic_and_replaces_missing_characters()
    {
        Assert.Equal(new byte[] { 0xC0, 0xFF, (byte)'x', (byte)'?', (byte)'?' }, Cp1251.GetBytes("Аяx\u00D7\u65E5"));
    }
}
