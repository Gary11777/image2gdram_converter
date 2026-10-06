using Image2Gdram.Core.Fonts;

namespace Image2Gdram.Core.Tests.Fonts;

/// <summary>Диапазоны символов п. 4.2.3 ТЗ (решение N-13, N-50).</summary>
public class CharRangeSetTests
{
    [Fact]
    public void Latin_preset_is_0x20_to_0x7E()
    {
        Assert.Equal(Enumerable.Range(0x20, 0x7E - 0x20 + 1), CharRangeSet.GetPresetCodes(CharRangePreset.Latin));
    }

    [Fact]
    public void Cyrillic_preset_is_0xC0_to_0xFF_plus_yo()
    {
        int[] expected = new[] { 0xA8, 0xB8 }.Concat(Enumerable.Range(0xC0, 64)).ToArray();
        Assert.Equal(expected, CharRangeSet.GetPresetCodes(CharRangePreset.Cyrillic));
    }

    [Fact]
    public void Other_preset_is_0x80_to_0xBF_without_0x98()
    {
        IReadOnlyList<int> codes = CharRangeSet.GetPresetCodes(CharRangePreset.OtherCp1251);
        Assert.Equal(63, codes.Count);
        Assert.Equal(0x80, codes[0]);
        Assert.Equal(0xBF, codes[^1]);
        Assert.DoesNotContain(0x98, codes);
    }

    [Fact]
    public void Presets_combine_without_duplicates_and_never_include_control_codes_or_0x98()
    {
        var all = new CharRangeSet(CharRangePreset.Latin | CharRangePreset.Cyrillic | CharRangePreset.OtherCp1251);
        Assert.Equal(95 + 63 + 64, all.Codes.Count);
        Assert.Equal(all.Codes.OrderBy(c => c).Distinct(), all.Codes);
        foreach (int code in Enumerable.Range(0, 0x20).Append(0x7F).Append(0x98))
        {
            Assert.False(all.Contains(code));
        }

        Assert.Equal(161, CharRangeSet.Default.Codes.Count);
        Assert.Equal(CharRangePreset.Latin | CharRangePreset.Cyrillic, CharRangeSet.Default.Presets);
        Assert.Empty(new CharRangeSet(CharRangePreset.None).Codes);
    }

    [Fact]
    public void Custom_codes_add_to_presets_including_explicit_0x98_and_control_codes()
    {
        var set = new CharRangeSet(CharRangePreset.Latin, new[] { 0x98, 0x01, 0x41, 0x01 });
        Assert.True(set.Contains(0x98));
        Assert.True(set.Contains(0x01));
        Assert.Equal(new[] { 0x01, 0x41, 0x98 }, set.CustomCodes);
        Assert.Equal(97, set.Codes.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharRangeSet(CharRangePreset.None, new[] { 256 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharRangeSet((CharRangePreset)8));
    }

    [Theory]
    [InlineData("", new int[0])]
    [InlineData("   ", new int[0])]
    [InlineData("0x41", new[] { 0x41 })]
    [InlineData("0X41", new[] { 0x41 })]
    [InlineData("41h", new[] { 0x41 })]
    [InlineData("0C0H", new[] { 0xC0 })]
    [InlineData("200", new[] { 200 })]
    [InlineData("0", new[] { 0 })]
    [InlineData("255", new[] { 255 })]
    [InlineData("0x41-0x44", new[] { 0x41, 0x42, 0x43, 0x44 })]
    [InlineData("0x41 - 0x43", new[] { 0x41, 0x42, 0x43 })]
    [InlineData("0x41-0x41", new[] { 0x41 })]
    [InlineData("0x41-0x43, 0xA8, 200", new[] { 0x41, 0x42, 0x43, 0xA8, 200 })]
    [InlineData("1 2,3,,4", new[] { 1, 2, 3, 4 })]
    [InlineData("5, 3-6, 4", new[] { 3, 4, 5, 6 })]
    [InlineData("0x0000FF", new[] { 255 })]
    [InlineData("0x97-0x99", new[] { 0x97, 0x98, 0x99 })]
    [InlineData("\t0x10\r\n0x11", new[] { 0x10, 0x11 })]
    public void Custom_range_is_parsed(string text, int[] expected)
    {
        Assert.True(CharRangeSet.TryParseCustom(text, out IReadOnlyList<int> codes, out CharRangeParseError? error));
        Assert.Null(error);
        Assert.Equal(expected, codes);
    }

    [Theory]
    [InlineData("abc", CharRangeParseErrorKind.InvalidNumber, 0, "abc")]
    [InlineData("0x", CharRangeParseErrorKind.InvalidNumber, 0, "0x")]
    [InlineData("0xG1", CharRangeParseErrorKind.InvalidNumber, 0, "0xG1")]
    [InlineData("C0h", CharRangeParseErrorKind.InvalidNumber, 0, "C0h")]
    [InlineData("h", CharRangeParseErrorKind.InvalidNumber, 0, "h")]
    [InlineData("12a", CharRangeParseErrorKind.InvalidNumber, 0, "12a")]
    [InlineData("1, 2, x", CharRangeParseErrorKind.InvalidNumber, 6, "x")]
    [InlineData("256", CharRangeParseErrorKind.OutOfRange, 0, "256")]
    [InlineData("0x100", CharRangeParseErrorKind.OutOfRange, 0, "0x100")]
    [InlineData("100h", CharRangeParseErrorKind.OutOfRange, 0, "100h")]
    [InlineData("99999999999999999999", CharRangeParseErrorKind.OutOfRange, 0, "99999999999999999999")]
    [InlineData("0x10-0x1FF", CharRangeParseErrorKind.OutOfRange, 5, "0x1FF")]
    [InlineData("0x50-0x41", CharRangeParseErrorKind.ReversedRange, 0, "0x50-0x41")]
    [InlineData("-5", CharRangeParseErrorKind.MisplacedDash, 0, "-")]
    [InlineData("5-", CharRangeParseErrorKind.MisplacedDash, 1, "-")]
    [InlineData("5--6", CharRangeParseErrorKind.MisplacedDash, 1, "-")]
    [InlineData("1-2-3", CharRangeParseErrorKind.MisplacedDash, 3, "-")]
    [InlineData("1, -3", CharRangeParseErrorKind.MisplacedDash, 3, "-")]
    [InlineData("1 -, 3", CharRangeParseErrorKind.MisplacedDash, 2, "-")]
    public void Invalid_custom_range_reports_kind_and_position(string text, CharRangeParseErrorKind kind, int position, string token)
    {
        Assert.False(CharRangeSet.TryParseCustom(text, out IReadOnlyList<int> codes, out CharRangeParseError? error));
        Assert.Empty(codes);
        Assert.Equal(new CharRangeParseError(kind, position, token), error);
    }
}
