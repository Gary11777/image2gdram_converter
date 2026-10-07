using Image2Gdram.TestAssets;

namespace Image2Gdram.Core.Tests.TestAssets;

/// <summary>????? ??????? ?????????? ? ????????: 16?16 ?????, ?????????? ????? 5?7 (N-60).</summary>
public class FontSheetTests
{
    [Fact]
    public void Three_sheets_are_sixteen_by_sixteen_cells()
    {
        Assert.Equal(new[] { "6x8", "8x8", "12x16" }, FontSheet.All.Select(sheet => $"{sheet.CellWidth}x{sheet.CellHeight}"));
        foreach (FontSheet sheet in FontSheet.All)
        {
            Assert.Equal(16 * sheet.CellWidth, sheet.Width);
            Assert.Equal(16 * sheet.CellHeight, sheet.Height);
            Assert.Equal("font_sheet_" + sheet.CellWidth + "x" + sheet.CellHeight + ".png", sheet.FileName);
        }
    }

    [Fact]
    public void Preview_phrase_has_ink_and_blank_codes_stay_blank()
    {
        FontSheet sheet = FontSheet.All[0];
        Assert.False(HasInk(sheet, 0x00));
        Assert.False(HasInk(sheet, 0x20));
        int[] phrase =
        [
            0xCF, 0xF0, 0xE8, 0xE2, 0xE5, 0xF2, 0x21,
            0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x31, 0x32, 0x33,
        ];
        foreach (int code in phrase)
        {
            Assert.True(HasInk(sheet, code), "0x" + code.ToString("X2"));
        }
    }

    [Fact]
    public void Digit_one_is_placed_by_the_cell_rule()
    {
        FontSheet narrow = FontSheet.All[0];
        Assert.True(narrow.IsCellInk(0x31, 2, 0));
        Assert.False(narrow.IsCellInk(0x31, 5, 0));
        Assert.False(narrow.IsCellInk(0x31, 0, 7));

        FontSheet square = FontSheet.All[1];
        Assert.False(square.IsCellInk(0x31, 0, 0));
        Assert.True(square.IsCellInk(0x31, 3, 0));

        FontSheet large = FontSheet.All[2];
        Assert.False(large.IsCellInk(0x31, 0, 0));
        Assert.True(large.IsCellInk(0x31, 5, 1));
        Assert.True(large.IsCellInk(0x31, 6, 2));
        Assert.False(large.IsCellInk(0x31, 4, 1));
    }

    [Fact]
    public void Unassigned_code_is_marked_by_its_bits_and_zero_is_empty()
    {
        FontSheet sheet = FontSheet.All[0];
        Assert.True(sheet.IsCellInk(0x01, 0, 0));
        Assert.False(sheet.IsCellInk(0x01, 1, 0));
        Assert.False(HasInk(sheet, 0x00));
    }

    private static bool HasInk(FontSheet sheet, int code)
    {
        for (int y = 0; y < sheet.CellHeight; y++)
        {
            for (int x = 0; x < sheet.CellWidth; x++)
            {
                if (sheet.IsCellInk(code, x, y))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
