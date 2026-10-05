using Image2Gdram.Core.Output;

namespace Image2Gdram.Core.Tests.Output;

/// <summary>Числа (D-09), комментарии символов (D-10, N-31) и разбиение строк (D-08).</summary>
public class FormattingTests
{
    [Theory]
    [InlineData(0x00, "0x00")]
    [InlineData(0x0F, "0x0F")]
    [InlineData(0x7C, "0x7C")]
    [InlineData(0xFF, "0xFF")]
    public void C_literals_are_upper_case_hex(int value, string expected)
    {
        Assert.Equal(expected, NumberFormatter.C((byte)value));
    }

    [Theory]
    [InlineData(0x00, "000h")]
    [InlineData(0x12, "012h")]
    [InlineData(0x7C, "07Ch")]
    [InlineData(0xC0, "0C0h")]
    [InlineData(0xFF, "0FFh")]
    public void Asm_hex_data_always_has_leading_zero_and_two_digits(int value, string expected)
    {
        Assert.Equal(expected, NumberFormatter.AsmHexData((byte)value));
        Assert.Equal(expected, NumberFormatter.Asm((byte)value, AsmNumberFormat.Hex));
    }

    [Theory]
    [InlineData(0x00, "00000000b")]
    [InlineData(0x05, "00000101b")]
    [InlineData(0x7C, "01111100b")]
    [InlineData(0xFF, "11111111b")]
    public void Asm_binary_data_has_eight_digits(int value, string expected)
    {
        Assert.Equal(expected, NumberFormatter.AsmBinaryData((byte)value));
        Assert.Equal(expected, NumberFormatter.Asm((byte)value, AsmNumberFormat.Binary));
    }

    [Theory]
    [InlineData(0x00, "00h")]
    [InlineData(0x7F, "7Fh")]
    [InlineData(0x9F, "9Fh")]
    [InlineData(0xA0, "0A0h")]
    [InlineData(0xC0, "0C0h")]
    [InlineData(0xFF, "0FFh")]
    public void Asm_code_has_leading_zero_only_before_letter(int code, string expected)
    {
        Assert.Equal(expected, NumberFormatter.AsmCode((byte)code));
    }

    [Theory]
    [InlineData(0x00, "/* 0x00 */", "; 00h")]
    [InlineData(0x0A, "/* 0x0A */", "; 0Ah")]
    [InlineData(0x20, "/* 0x20 ' ' */", "; 20h ' '")]
    [InlineData(0x2A, "/* 0x2A '*' */", "; 2Ah '*'")]
    [InlineData(0x2F, "/* 0x2F '/' */", "; 2Fh '/'")]
    [InlineData(0x5C, "/* 0x5C '\\' */", "; 5Ch '\\'")]
    [InlineData(0x41, "/* 0x41 'A' */", "; 41h 'A'")]
    [InlineData(0x7F, "/* 0x7F */", "; 7Fh")]
    [InlineData(0x98, "/* 0x98 */", "; 98h")]
    [InlineData(0xA0, "/* 0xA0 */", "; 0A0h")]
    [InlineData(0xAD, "/* 0xAD */", "; 0ADh")]
    [InlineData(0xA8, "/* 0xA8 'Ё' */", "; 0A8h 'Ё'")]
    [InlineData(0xC0, "/* 0xC0 'А' */", "; 0C0h 'А'")]
    [InlineData(0xFF, "/* 0xFF 'я' */", "; 0FFh 'я'")]
    public void Glyph_comments_show_printable_characters_only(int code, string c, string asm)
    {
        Assert.Equal(c, GlyphCommentFormatter.C((byte)code));
        Assert.Equal(asm, GlyphCommentFormatter.Asm((byte)code));
    }

    [Fact]
    public void Glyph_comments_never_open_or_close_a_c_comment_early()
    {
        for (int code = 0; code < 256; code++)
        {
            string inner = GlyphCommentFormatter.CText((byte)code);
            Assert.DoesNotContain("*/", inner);
            Assert.DoesNotContain("/*", inner);
        }
    }

    [Theory]
    [InlineData(6, new[] { 6 })]
    [InlineData(8, new[] { 8 })]
    [InlineData(16, new[] { 16 })]
    [InlineData(17, new[] { 9, 8 })]
    [InlineData(24, new[] { 12, 12 })]
    [InlineData(32, new[] { 16, 16 })]
    [InlineData(33, new[] { 11, 11, 11 })]
    [InlineData(40, new[] { 14, 13, 13 })]
    public void Glyph_lines_are_split_evenly(int bytesPerChar, int[] expected)
    {
        Assert.Equal(expected, DataLayout.SplitGlyph(bytesPerChar));
    }

    [Theory]
    [InlineData(1024, 16, 64, 16)]
    [InlineData(13, 5, 3, 3)]
    [InlineData(7, 16, 1, 7)]
    [InlineData(5, 1, 5, 1)]
    public void Image_lines_have_bytes_per_line_and_remainder(int count, int perLine, int lines, int last)
    {
        int[] lengths = DataLayout.SplitImage(count, perLine);

        Assert.Equal(lines, lengths.Length);
        Assert.Equal(last, lengths[^1]);
        Assert.All(lengths[..^1], l => Assert.Equal(perLine, l));
        Assert.Equal(count, lengths.Sum());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public void Bytes_per_line_outside_1_to_16_is_rejected(int perLine)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DataLayout.SplitImage(10, perLine));
    }
}
