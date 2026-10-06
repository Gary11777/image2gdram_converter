using Image2Gdram.Core.Editing;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Tests.Packing;
using static Image2Gdram.Core.Tests.Fonts.Polygons;

namespace Image2Gdram.Core.Tests.Fonts;

/// <summary>Операции редактора символа и их отмена (п. 5.4 ТЗ, решения D-14, N-14, N-22, N-46).</summary>
public class GlyphEditingTests
{
    private static readonly MonoBitmap Sample = TestBitmaps.FromRows(
        "#..",
        ".#.",
        "..#",
        "##.");

    [Fact]
    public void Shift_moves_by_one_pixel_and_fills_with_background()
    {
        Assert.Equal(new[] { "...", "#..", ".#.", "..#" }, Rows(GlyphOps.Shift(Sample, ShiftDirection.Down)));
        Assert.Equal(new[] { ".#.", "..#", "##.", "..." }, Rows(GlyphOps.Shift(Sample, ShiftDirection.Up)));
        Assert.Equal(new[] { ".#.", "..#", "...", ".##" }, Rows(GlyphOps.Shift(Sample, ShiftDirection.Right)));
        Assert.Equal(new[] { "...", "#..", ".#.", "#.." }, Rows(GlyphOps.Shift(Sample, ShiftDirection.Left)));
    }

    [Fact]
    public void Shifting_out_and_back_loses_the_edge()
    {
        MonoBitmap back = GlyphOps.Shift(GlyphOps.Shift(Sample, ShiftDirection.Left), ShiftDirection.Right);
        Assert.Equal(new[] { "...", ".#.", "..#", ".#." }, Rows(back));
    }

    [Fact]
    public void Clear_invert_and_toggle_return_new_bitmaps()
    {
        Assert.All(Rows(GlyphOps.Clear(Sample)), row => Assert.Equal("...", row));
        Assert.Equal(new[] { ".##", "#.#", "##.", "..#" }, Rows(GlyphOps.Invert(Sample)));
        Assert.Equal(new[] { "##.", ".#.", "..#", "##." }, Rows(GlyphOps.Toggle(Sample, 1, 0)));
        Assert.Equal(new[] { "#..", ".#.", "..#", "##." }, Rows(Sample));
        Assert.Throws<ArgumentOutOfRangeException>(() => GlyphOps.Toggle(Sample, 3, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => GlyphOps.Shift(Sample, (ShiftDirection)8));
    }

    [Fact]
    public void Fit_places_another_size_in_the_top_left_corner()
    {
        MonoBitmap big = TestBitmaps.Filled(10, 10, true);
        Assert.All(Rows(GlyphOps.Fit(big, FontCellSize.Cell6x8)), row => Assert.Equal("######", row));
        string[] small = Rows(GlyphOps.Fit(Sample, FontCellSize.Cell6x8));
        Assert.Equal(new[] { "#.....", ".#....", "..#...", "##....", "......", "......", "......", "......" }, small);
    }

    [Fact]
    public void Edit_action_is_undone_and_redone()
    {
        var table = new FontTable(FontCellSize.Cell6x8);
        var source = new GlyphSet(FontCellSize.Cell6x8);
        source.Set(0x41, FontTableTests.Pattern(1));
        table.ReplaceSource(source);
        var history = new EditHistory();

        MonoBitmap edited = GlyphOps.Invert(table.GetGlyph(0x41));
        GlyphEditAction action = GlyphEditAction.Apply(table, 0x41, edited)!;
        history.Push(action);
        Assert.Equal(GlyphOrigin.Manual, table.GetOrigin(0x41));
        Assert.True(table.GetGlyph(0x41).ContentEquals(edited));

        history.Undo();
        Assert.Equal(GlyphOrigin.Source, table.GetOrigin(0x41));
        Assert.True(table.GetGlyph(0x41).ContentEquals(FontTableTests.Pattern(1)));

        history.Redo();
        Assert.True(table.GetGlyph(0x41).ContentEquals(edited));
    }

    [Fact]
    public void Revert_to_source_is_an_undoable_action()
    {
        var table = new FontTable(FontCellSize.Cell6x8);
        MonoBitmap manual = FontTableTests.Pattern(4);
        table.SetManual(0x05, manual);

        GlyphEditAction revert = GlyphEditAction.Apply(table, 0x05, null)!;
        Assert.Equal(GlyphOrigin.None, table.GetOrigin(0x05));
        revert.Undo();
        Assert.True(table.GetManualGlyph(0x05)!.ContentEquals(manual));
        revert.Redo();
        Assert.False(table.IsManual(0x05));
    }

    [Fact]
    public void Edits_that_change_nothing_are_not_recorded()
    {
        var table = new FontTable(FontCellSize.Cell6x8);
        Assert.Null(GlyphEditAction.Apply(table, 0x41, null));
        table.SetManual(0x41, FontTableTests.Pattern(2));
        Assert.Null(GlyphEditAction.Apply(table, 0x41, FontTableTests.Pattern(2)));
    }

    [Fact]
    public void Action_keeps_its_own_copy_of_the_bitmap()
    {
        var table = new FontTable(FontCellSize.Cell6x8);
        MonoBitmap edited = FontTableTests.Pattern(6);
        GlyphEditAction action = GlyphEditAction.Apply(table, 0x30, edited)!;
        edited[0, 0] = !edited[0, 0];
        action.Undo();
        action.Redo();
        Assert.True(table.GetGlyph(0x30).ContentEquals(FontTableTests.Pattern(6)));
    }
}
