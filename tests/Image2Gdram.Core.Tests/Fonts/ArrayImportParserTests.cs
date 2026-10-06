using Image2Gdram.Core.Fonts.Import;

namespace Image2Gdram.Core.Tests.Fonts;

/// <summary>Разбор массивов C и A51 (п. 4.2.2 ТЗ, источник 4; решения D-17, N-52).</summary>
public class ArrayImportParserTests
{
    [Theory]
    [InlineData(ImportSyntax.C, "0x1F", 0x1F)]
    [InlineData(ImportSyntax.C, "0X1F", 0x1F)]
    [InlineData(ImportSyntax.C, "0x1f", 0x1F)]
    [InlineData(ImportSyntax.C, "0x0", 0)]
    [InlineData(ImportSyntax.C, "0x00FF", 255)]
    [InlineData(ImportSyntax.C, "1Fh", 0x1F)]
    [InlineData(ImportSyntax.C, "1fH", 0x1F)]
    [InlineData(ImportSyntax.C, "0C0h", 0xC0)]
    [InlineData(ImportSyntax.C, "0FFh", 0xFF)]
    [InlineData(ImportSyntax.C, "00h", 0)]
    [InlineData(ImportSyntax.C, "0Bh", 0x0B)]
    [InlineData(ImportSyntax.C, "1Bh", 0x1B)]
    [InlineData(ImportSyntax.C, "01010101b", 0b01010101)]
    [InlineData(ImportSyntax.C, "0101B", 0b0101)]
    [InlineData(ImportSyntax.C, "0b01010101", 0b01010101)]
    [InlineData(ImportSyntax.C, "0B1", 1)]
    [InlineData(ImportSyntax.C, "0b", 0)]
    [InlineData(ImportSyntax.C, "000000001b", 1)]
    [InlineData(ImportSyntax.C, "0", 0)]
    [InlineData(ImportSyntax.C, "255", 255)]
    [InlineData(ImportSyntax.C, "7", 7)]
    [InlineData(ImportSyntax.C, "010", 8)]
    [InlineData(ImportSyntax.C, "0377", 255)]
    [InlineData(ImportSyntax.C, "0x1Fu", 0x1F)]
    [InlineData(ImportSyntax.C, "0x1FU", 0x1F)]
    [InlineData(ImportSyntax.C, "255UL", 255)]
    [InlineData(ImportSyntax.C, "1l", 1)]
    [InlineData(ImportSyntax.Asm, "0x1F", 0x1F)]
    [InlineData(ImportSyntax.Asm, "0C0h", 0xC0)]
    [InlineData(ImportSyntax.Asm, "0FFh", 255)]
    [InlineData(ImportSyntax.Asm, "01010101b", 0b01010101)]
    [InlineData(ImportSyntax.Asm, "0b01010101", 0b01010101)]
    [InlineData(ImportSyntax.Asm, "0b", 0)]
    [InlineData(ImportSyntax.Asm, "000000001b", 1)]
    [InlineData(ImportSyntax.Asm, "010", 10)]
    [InlineData(ImportSyntax.Asm, "08", 8)]
    [InlineData(ImportSyntax.Asm, "255", 255)]
    public void Number_literals_have_the_defined_value(ImportSyntax syntax, string literal, int expected)
    {
        ImportedArray array = Sole(Snippet(syntax, literal), syntax);
        Assert.Null(array.Error);
        Assert.Equal(new[] { (byte)expected }, array.Values.ToArray());
    }

    [Theory]
    [InlineData(ImportSyntax.C, "0x", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.C, "0xG1", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.C, "0x100", ArrayImportErrorKind.ValueOutOfRange)]
    [InlineData(ImportSyntax.C, "C0h", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.C, "FFh", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.C, "100h", ArrayImportErrorKind.ValueOutOfRange)]
    [InlineData(ImportSyntax.C, "0102b", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.C, "0b102", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.C, "111111111b", ArrayImportErrorKind.ValueOutOfRange)]
    [InlineData(ImportSyntax.C, "256", ArrayImportErrorKind.ValueOutOfRange)]
    [InlineData(ImportSyntax.C, "-1", ArrayImportErrorKind.ValueOutOfRange)]
    [InlineData(ImportSyntax.C, "+5", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.C, "1+2", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.C, "LOW(X)", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.C, "$", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.C, "'A'", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.C, "(unsigned char)0x1F", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.C, "08", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.Asm, "0x", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.Asm, "C0h", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.Asm, "100h", ArrayImportErrorKind.ValueOutOfRange)]
    [InlineData(ImportSyntax.Asm, "0102b", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.Asm, "0b102", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.Asm, "111111111b", ArrayImportErrorKind.ValueOutOfRange)]
    [InlineData(ImportSyntax.Asm, "256", ArrayImportErrorKind.ValueOutOfRange)]
    [InlineData(ImportSyntax.Asm, "-1", ArrayImportErrorKind.ValueOutOfRange)]
    [InlineData(ImportSyntax.Asm, "+5", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.Asm, "255UL", ArrayImportErrorKind.InvalidNumber)]
    [InlineData(ImportSyntax.Asm, "1l", ArrayImportErrorKind.InvalidNumber)]
    public void Invalid_numbers_keep_the_line_and_the_lexeme(ImportSyntax syntax, string literal, ArrayImportErrorKind kind)
    {
        ImportedArray array = Sole(ErrorSnippet(syntax, literal), syntax);
        Assert.NotNull(array.Error);
        Assert.Equal(kind, array.Error.Kind);
        Assert.Equal(2, array.Error.Line);
        Assert.Equal(literal, array.Error.Token);
        Assert.Empty(array.Values);
    }

    [Fact]
    public void Comments_of_both_syntaxes_do_not_hide_real_values()
    {
        ImportedArray c = Sole("unsigned char a[] = { /* { } = DB ; 1 0xFF // */ 2, // 3\n 4 };", ImportSyntax.C);
        Assert.Equal(new byte[] { 2, 4 }, c.Values.ToArray());

        IReadOnlyList<ImportedArray> sameLine = ArrayImportParser.Parse("unsigned char a[] = {1}; unsigned char b[] = {2};", ImportSyntax.C);
        Assert.Equal(new[] { "a", "b" }, sameLine.Select(a => a.Name).ToArray());
        Assert.Equal(new byte[] { 1 }, sameLine[0].Values.ToArray());
        Assert.Equal(new byte[] { 2 }, sameLine[1].Values.ToArray());

        ImportedArray asm = Sole("DB 1 /* ; DB 9\n // still */ , 2\n; only a comment\nDB 3 ; DB 4", ImportSyntax.Asm);
        Assert.Equal(new byte[] { 1, 2, 3 }, asm.Values.ToArray());

        ImportedArray afterLineComment = Sole("// /*\nunsigned char a[] = {8};", ImportSyntax.C);
        Assert.Equal(new byte[] { 8 }, afterLineComment.Values.ToArray());
    }

    [Theory]
    [InlineData("\r\n")]
    [InlineData("\n")]
    [InlineData("\r")]
    public void Line_numbers_count_through_multiline_comments(string newline)
    {
        string text = "/*" + newline + "x" + newline + "*/" + newline + "unsigned char a[] = {7};";
        ImportedArray array = Sole(text, ImportSyntax.C);
        Assert.Equal(4, array.Line);
        Assert.Equal(new byte[] { 7 }, array.Values.ToArray());

        string open = "int x;" + newline + "/* start" + newline + "no end";
        ArrayImportException ex = Assert.Throws<ArrayImportException>(() => ArrayImportParser.Parse(open, ImportSyntax.C));
        Assert.Equal(ArrayImportErrorKind.UnterminatedComment, ex.Kind);
        Assert.Equal(2, ex.Line);
        Assert.Equal("/*", ex.Token);
    }

    [Fact]
    public void C_declarations_keep_the_last_identifier_before_the_brackets()
    {
        const string text = """
            #define FONT = {9, 9}
            extern unsigned char font[];
            int x = 5;
            int y = "str";
            int z = &x;
            #if 0
            unsigned char hidden[] = {1};
            #endif
            const static unsigned char code PROGMEM font_6x8[256][6] = {1, 2};
            __attribute__((aligned(4))) const uint8_t image[] = {3};
            unsigned char __attribute__((section(".rom"))) font[8][8] = {4};
            void f(void) {
                int local = 1;
                unsigned char inner[] = {5, 6};
            }
            """;

        IReadOnlyList<ImportedArray> arrays = ArrayImportParser.Parse(text.Replace("\r\n", "\n", StringComparison.Ordinal), ImportSyntax.C);
        Assert.Equal(new[] { "hidden", "font_6x8", "image", "font", "inner" }, arrays.Select(a => a.Name).ToArray());
        Assert.Equal(new byte[][] { new byte[] { 1 }, new byte[] { 1, 2 }, new byte[] { 3 }, new byte[] { 4 }, new byte[] { 5, 6 } }, arrays.Select(a => a.Values.ToArray()).ToArray());
        Assert.All(arrays, a => Assert.Equal(ImportedArrayKind.CInitializer, a.Kind));
    }

    [Fact]
    public void Nested_braces_flatten_and_trailing_commas_are_allowed()
    {
        ImportedArray nested = Sole("unsigned char a[] = {{{1, 2}, {3, {4}}}};", ImportSyntax.C);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, nested.Values.ToArray());

        ImportedArray empty = Sole("unsigned char empty[] = {};", ImportSyntax.C);
        Assert.Empty(empty.Values);
        Assert.Null(empty.Error);

        ImportedArray trailing = Sole("unsigned char a[] = {1, 2,};", ImportSyntax.C);
        Assert.Equal(new byte[] { 1, 2 }, trailing.Values.ToArray());
        ImportedArray trailingNested = Sole("unsigned char a[] = {{1},};", ImportSyntax.C);
        Assert.Equal(new byte[] { 1 }, trailingNested.Values.ToArray());

        ImportedArray hole = Sole("unsigned char a[] = {\n1,,2\n};", ImportSyntax.C);
        Assert.NotNull(hole.Error);
        Assert.Equal(ArrayImportErrorKind.InvalidNumber, hole.Error.Kind);
        Assert.Equal(string.Empty, hole.Error.Token);
        Assert.Empty(hole.Values);
    }

    [Fact]
    public void Unbalanced_braces_name_the_line()
    {
        ArrayImportException open = Assert.Throws<ArrayImportException>(() => ArrayImportParser.Parse("unsigned char a[] = {1, 2;", ImportSyntax.C));
        Assert.Equal(ArrayImportErrorKind.UnbalancedBraces, open.Kind);
        Assert.Equal(1, open.Line);
        Assert.Equal("{", open.Token);

        ArrayImportException extra = Assert.Throws<ArrayImportException>(() => ArrayImportParser.Parse("unsigned char a[] = {1};\n}", ImportSyntax.C));
        Assert.Equal(ArrayImportErrorKind.UnbalancedBraces, extra.Kind);
        Assert.Equal(2, extra.Line);
        Assert.Equal("}", extra.Token);
    }

    [Fact]
    public void C_name_line_is_the_identifier_line()
    {
        ImportedArray array = Sole("unsigned char\nfont_6x8[] = {1};", ImportSyntax.C);
        Assert.Equal("font_6x8", array.Name);
        Assert.Equal(2, array.Line);
    }

    [Fact]
    public void Asm_labels_and_db_sequences_follow_the_defined_breaks()
    {
        const string text = """
            PUBLIC FONT
            FONT_6X8:
                ; comment

                DB 1, 2,

                db 3
            OTHER:
                DB 4
            BARE DB 5
                DB 6
            DW 7
            DB 8
            MOV A, #1
            $INCLUDE (font.inc)
            END
            DB 9
            """;

        IReadOnlyList<ImportedArray> arrays = ArrayImportParser.Parse(text.Replace("\r\n", "\n", StringComparison.Ordinal), ImportSyntax.Asm);
        Assert.Equal(new string?[] { "FONT_6X8", "OTHER", "BARE", null, null }, arrays.Select(a => a.Name).ToArray());
        Assert.Equal(
            new byte[][] { new byte[] { 1, 2, 3 }, new byte[] { 4 }, new byte[] { 5, 6 }, new byte[] { 8 }, new byte[] { 9 } },
            arrays.Select(a => a.Values.ToArray()).ToArray());
        Assert.All(arrays, a => Assert.Equal(ImportedArrayKind.AsmDb, a.Kind));
        Assert.Equal(2, arrays[0].Line);
    }

    [Fact]
    public void Asm_strings_use_cp1251_and_doubled_quotes()
    {
        ImportedArray mixed = Sole("DB 'AB', 0, \"B\", 'It''s', ''", ImportSyntax.Asm);
        Assert.Equal(new byte[] { 0x41, 0x42, 0, 0x42, 0x49, 0x74, 0x27, 0x73 }, mixed.Values.ToArray());

        ImportedArray cyrillic = Sole("DB '\u0410'", ImportSyntax.Asm);
        Assert.Equal(new byte[] { 0xC0 }, cyrillic.Values.ToArray());

        ImportedArray unassigned = Sole("DB '\u0098'", ImportSyntax.Asm);
        Assert.Equal(new byte[] { 0x98 }, unassigned.Values.ToArray());

        ImportedArray inside = Sole("DB 'A;B//C', 1", ImportSyntax.Asm);
        Assert.Equal(new byte[] { 0x41, 0x3B, 0x42, 0x2F, 0x2F, 0x43, 1 }, inside.Values.ToArray());

        ImportedArray bad = Sole("\nDB '\u0100'", ImportSyntax.Asm);
        Assert.NotNull(bad.Error);
        Assert.Equal(ArrayImportErrorKind.CharacterNotInCp1251, bad.Error.Kind);
        Assert.Equal(2, bad.Error.Line);
        Assert.Equal("\u0100", bad.Error.Token);
        Assert.Empty(bad.Values);

        ArrayImportException open = Assert.Throws<ArrayImportException>(() => ArrayImportParser.Parse("DB 1\nDB 'AB", ImportSyntax.Asm));
        Assert.Equal(ArrayImportErrorKind.UnterminatedString, open.Kind);
        Assert.Equal(2, open.Line);
        Assert.Equal("'", open.Token);
    }

    [Fact]
    public void Asm_label_without_a_previous_directive_uses_the_db_line_when_unnamed()
    {
        ImportedArray unnamed = Sole("\nDB 1, 2", ImportSyntax.Asm);
        Assert.Null(unnamed.Name);
        Assert.Equal(2, unnamed.Line);
        Assert.Equal(new byte[] { 1, 2 }, unnamed.Values.ToArray());
    }

    [Fact]
    public void Several_arrays_keep_order_and_an_error_does_not_hide_the_rest()
    {
        IReadOnlyList<ImportedArray> c = ArrayImportParser.Parse(
            "unsigned char a[] = {1, 0x100, 2};\nunsigned char a[] = {3, 4};\nunsigned char b[] = {5};",
            ImportSyntax.C);
        Assert.Equal(new[] { "a", "a", "b" }, c.Select(a => a.Name).ToArray());
        Assert.Equal(1, c[0].Line);
        ArrayImportIssue cError = c[0].Error!;
        Assert.Equal(ArrayImportErrorKind.ValueOutOfRange, cError.Kind);
        Assert.Equal("0x100", cError.Token);
        Assert.Empty(c[0].Values);
        Assert.Equal(new byte[] { 3, 4 }, c[1].Values.ToArray());
        Assert.Equal(2, c[1].Values.Count);
        Assert.Equal(new byte[] { 5 }, c[2].Values.ToArray());

        IReadOnlyList<ImportedArray> asm = ArrayImportParser.Parse("A:\nDB 1, 300\nB:\nDB 2, 3\n", ImportSyntax.Asm);
        Assert.Equal(new[] { "A", "B" }, asm.Select(a => a.Name).ToArray());
        ArrayImportIssue asmError = asm[0].Error!;
        Assert.Equal(ArrayImportErrorKind.ValueOutOfRange, asmError.Kind);
        Assert.Equal("300", asmError.Token);
        Assert.Empty(asm[0].Values);
        Assert.Equal(new byte[] { 2, 3 }, asm[1].Values.ToArray());
        Assert.Null(asm[1].Error);
    }

    [Fact]
    public void Empty_text_has_no_arrays_and_null_text_throws()
    {
        Assert.Empty(ArrayImportParser.Parse(string.Empty, ImportSyntax.C));
        Assert.Empty(ArrayImportParser.Parse("   \n  ; comment\n", ImportSyntax.Asm));
        Assert.Throws<ArgumentNullException>(() => ArrayImportParser.Parse(null!, ImportSyntax.C));
        Assert.Throws<ArgumentOutOfRangeException>(() => ArrayImportParser.Parse(string.Empty, (ImportSyntax)99));
    }

    [Fact]
    public void Appendix_examples_parse()
    {
        IReadOnlyList<ImportedArray> header = ArrayImportParser.Parse(AppendixV1Header, ImportSyntax.C);
        Assert.Empty(header);

        ImportedArray c = Sole(AppendixV1Source, ImportSyntax.C);
        Assert.Equal("font_12x16", c.Name);
        Assert.Equal(Enumerable.Repeat((byte)0, 24).ToArray(), c.Values.ToArray());

        ImportedArray module = Sole(AppendixV2, ImportSyntax.Asm);
        Assert.Equal("FONT_6X8", module.Name);
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 0, 0x7C, 0x12, 0x11, 0x12, 0x7C, 0 }, module.Values.ToArray());

        ImportedArray include = Sole(AppendixV3, ImportSyntax.Asm);
        Assert.Equal("FONT_6X8", include.Name);
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 0 }, include.Values.ToArray());
    }

    private static string Snippet(ImportSyntax syntax, string literal) =>
        syntax == ImportSyntax.C ? "unsigned char a[] = {" + literal + "};" : "DB " + literal;

    private static string ErrorSnippet(ImportSyntax syntax, string literal) =>
        syntax == ImportSyntax.C ? "unsigned char a[] = {\n" + literal + "\n};" : "\nDB " + literal;

    private static ImportedArray Sole(string text, ImportSyntax syntax)
    {
        IReadOnlyList<ImportedArray> arrays = ArrayImportParser.Parse(text, syntax);
        return Assert.Single(arrays);
    }

    private const string AppendixV1Header = """
        #ifndef FONT_12X16_H
        #define FONT_12X16_H

        #define FONT_12X16_CHAR_WIDTH      12
        #define FONT_12X16_CHAR_HEIGHT     16
        #define FONT_12X16_BYTES_PER_CHAR  24

        extern unsigned char code font_12x16[256][FONT_12X16_BYTES_PER_CHAR];

        #endif
        """;

    private const string AppendixV1Source = """
        /*
         * Converter_image_IU 1.0
         * Шрифт: Consolas, 16 px; ячейка 12x16 (Ш×В)
         * Упаковка: вертикальная, LSB first, по страницам, инверсия: нет
         * Размер: 256 символов × 24 байта = 6144 байта
         */
        #include "font_12x16.h"

        unsigned char code font_12x16[256][FONT_12X16_BYTES_PER_CHAR] = {
            { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
              0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, /* 0x00 */
            /* ... и так далее до 256 символов ... */
        };
        """;

    private const string AppendixV2 = """
        ; Converter_image_IU 1.0
        ; Шрифт: ячейка 6x8 (Ш×В), 256 символов × 6 байт = 1536 байт
        ; Упаковка: вертикальная, LSB first, инверсия: нет

                        PUBLIC  FONT_6X8

        ?CO?FONT_6X8    SEGMENT CODE
                        RSEG    ?CO?FONT_6X8

        FONT_6X8:
                        DB      000h, 000h, 000h, 000h, 000h, 000h  ; 00h
                        ; ... и так далее до 256 символов ...
                        DB      07Ch, 012h, 011h, 012h, 07Ch, 000h  ; 0C0h 'А'
                        ; ...
                        END
        """;

    private const string AppendixV3 = """
        FONT_6X8:
                        DB      000h, 000h, 000h, 000h, 000h, 000h  ; 00h
                        ; ... и так далее до 256 символов ...
        """;
}
