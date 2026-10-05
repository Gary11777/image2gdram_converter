using Image2Gdram.Core.Output;

namespace Image2Gdram.Core.Tests.Output;

/// <summary>Имя массива: проверка (п. 4.4.2 ТЗ, N-05, N-06) и имя по умолчанию (D-13).</summary>
public class NameTests
{
    [Theory]
    [InlineData("logo_128x64")]
    [InlineData("font_6x8")]
    [InlineData("FONT_6X8")]
    [InlineData("_x")]
    [InlineData("x")]
    [InlineData("image2")]
    public void Valid_names_pass_for_every_format(string name)
    {
        foreach (OutputFormat format in OutputTestData.AllFormats)
        {
            Assert.True(NameValidator.Validate(name, format).IsValid);
        }
    }

    [Theory]
    [InlineData(null, NameValidationError.Empty)]
    [InlineData("", NameValidationError.Empty)]
    [InlineData("лого", NameValidationError.InvalidCharacter)]
    [InlineData("a-b", NameValidationError.InvalidCharacter)]
    [InlineData("a b", NameValidationError.InvalidCharacter)]
    [InlineData("a$", NameValidationError.InvalidCharacter)]
    [InlineData("1abc", NameValidationError.StartsWithDigit)]
    [InlineData("int", NameValidationError.CKeyword)]
    [InlineData("static", NameValidationError.CKeyword)]
    [InlineData("_Bool", NameValidationError.CKeyword)]
    [InlineData("code", NameValidationError.KeilC51Keyword)]
    [InlineData("xdata", NameValidationError.KeilC51Keyword)]
    [InlineData("_at_", NameValidationError.KeilC51Keyword)]
    [InlineData("__x", NameValidationError.ReservedCIdentifier)]
    [InlineData("_Logo", NameValidationError.ReservedCIdentifier)]
    [InlineData("uint8_t", NameValidationError.StdintTypeName)]
    [InlineData("int_fast16_t", NameValidationError.StdintTypeName)]
    [InlineData("r0", NameValidationError.A51ReservedWord)]
    [InlineData("Dptr", NameValidationError.A51ReservedWord)]
    [InlineData("MOVX", NameValidationError.A51ReservedWord)]
    [InlineData("db", NameValidationError.A51ReservedWord)]
    [InlineData("segment", NameValidationError.A51ReservedWord)]
    [InlineData("Code", NameValidationError.A51ReservedWord)]
    [InlineData("a", NameValidationError.A51ReservedWord)]
    [InlineData("name", NameValidationError.A51ReservedWord)]
    public void Invalid_names_are_rejected_with_reason(string? name, NameValidationError expected)
    {
        Assert.Equal(expected, NameValidator.Validate(name, OutputFormat.CStm32).Error);
    }

    [Fact]
    public void Length_limit_is_31_and_27_for_a51_module()
    {
        Assert.True(NameValidator.Validate(new string('a', 31), OutputFormat.CKeilC51).IsValid);
        Assert.Equal(NameValidationError.TooLong, NameValidator.Validate(new string('a', 32), OutputFormat.CKeilC51).Error);
        Assert.True(NameValidator.Validate(new string('a', 31), OutputFormat.A51Include).IsValid);

        NameValidationResult module28 = NameValidator.Validate(new string('a', 28), OutputFormat.A51Module);
        Assert.Equal(NameValidationError.TooLong, module28.Error);
        Assert.Equal(27, module28.MaxLength);
        Assert.True(NameValidator.Validate(new string('a', 27), OutputFormat.A51Module).IsValid);
    }

    [Fact]
    public void Every_reserved_word_is_rejected()
    {
        IEnumerable<string> all = ReservedWords.CKeywords
            .Concat(ReservedWords.KeilC51Keywords)
            .Concat(ReservedWords.StdintTypeNames);
        foreach (string word in all)
        {
            Assert.False(NameValidator.Validate(word, OutputFormat.CStm32).IsValid, word);
        }

        foreach (string word in ReservedWords.A51Words)
        {
            Assert.False(NameValidator.Validate(word.ToUpperInvariant(), OutputFormat.A51Module).IsValid, word);
            Assert.False(NameValidator.Validate(word.ToLowerInvariant(), OutputFormat.A51Module).IsValid, word);
        }
    }

    [Fact]
    public void Reserved_lists_contain_n06_entries()
    {
        Assert.Equal(37, ReservedWords.CKeywords.Count);
        Assert.Equal(21, ReservedWords.KeilC51Keywords.Count);
        Assert.Equal(28, ReservedWords.StdintTypeNames.Count);
        foreach (string word in new[] { "A", "AB", "C", "DPTR", "PC", "R7", "AR0", "AR7", "ACALL", "XRL", "PUBLIC", "RSEG", "END", "HIGH", "NUL" })
        {
            Assert.Contains(word, ReservedWords.A51Words);
        }
    }

    [Theory]
    [InlineData("logo", 128, 64, ArrayNameKind.Image, "logo_128x64")]
    [InlineData(null, 6, 8, ArrayNameKind.Font, "font_6x8")]
    [InlineData("", 128, 64, ArrayNameKind.Image, "image_128x64")]
    [InlineData("Consolas", 12, 16, ArrayNameKind.Font, "consolas_12x16")]
    [InlineData("Courier New", 8, 8, ArrayNameKind.Font, "courier_new_8x8")]
    [InlineData("My--Logo!!", 240, 128, ArrayNameKind.Image, "my_logo_240x128")]
    [InlineData("__a__b__", 13, 11, ArrayNameKind.Image, "a_b_13x11")]
    [InlineData("3d", 16, 16, ArrayNameKind.Image, "img_3d_16x16")]
    [InlineData("логотип", 128, 64, ArrayNameKind.Image, "image_128x64")]
    [InlineData("Лого 2", 128, 64, ArrayNameKind.Image, "img_2_128x64")]
    public void Default_name_follows_d13(string? baseName, int width, int height, ArrayNameKind kind, string expected)
    {
        Assert.Equal(expected, DefaultNameBuilder.Build(baseName, width, height, kind));
    }

    [Fact]
    public void Default_name_from_file_drops_folder_and_extension()
    {
        Assert.Equal("logo_main_128x64", DefaultNameBuilder.FromFile(@"C:\Images\Logo Main.PNG", 128, 64, ArrayNameKind.Image));
    }

    [Fact]
    public void Long_default_name_is_truncated_keeping_the_size_suffix()
    {
        string name = DefaultNameBuilder.Build("a_very_long_file_name_for_the_logo_image", 1024, 1024, ArrayNameKind.Image);

        Assert.Equal("a_very_long_file_name_1024x1024", name);
        Assert.Equal(31, name.Length);

        string module = DefaultNameBuilder.Build("a_very_long_file_name_for_the_logo_image", 1024, 1024, ArrayNameKind.Image, NameValidator.MaxLengthA51Module);
        Assert.Equal("a_very_long_file_1024x1024", module);
        Assert.True(NameValidator.Validate(module, OutputFormat.A51Module).IsValid);
    }

    [Theory]
    [InlineData("code")]
    [InlineData("int")]
    [InlineData("R0")]
    [InlineData("12345")]
    [InlineData("±±±")]
    [InlineData("x_______________________________________________y")]
    public void Default_name_always_passes_the_validator(string baseName)
    {
        foreach (OutputFormat format in OutputTestData.AllFormats)
        {
            string name = DefaultNameBuilder.Build(baseName, 240, 128, ArrayNameKind.Image, NameValidator.GetMaxLength(format));
            Assert.True(NameValidator.Validate(name, format).IsValid, name);
        }
    }
}
