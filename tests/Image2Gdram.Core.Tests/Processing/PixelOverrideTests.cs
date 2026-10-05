using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Tests.Processing;

public class PixelOverrideTests
{
    [Fact]
    public void Entries_are_sorted_by_row_then_column_and_outside_pixels_are_skipped()
    {
        var edits = new PixelOverrides();
        edits.Set(2, 1, true);
        edits.Set(0, 1, false);
        edits.Set(1, 0, true);
        edits.Set(-1, 0, true);
        edits.Set(9, 9, false);

        Assert.Equal(new[] { (1, 0, true), (-1, 0, true), (0, 1, false), (2, 1, true), (9, 9, false) }, Sorted(edits));

        var bitmap = new MonoBitmap(3, 2);
        edits.Apply(bitmap);
        Assert.True(bitmap[1, 0]);
        Assert.False(bitmap[0, 1]);
        Assert.True(bitmap[2, 1]);
    }

    [Fact]
    public void Remove_and_clear_drop_edits()
    {
        var edits = new PixelOverrides();
        edits.Set(0, 0, true);
        Assert.True(edits.Remove(0, 0));
        Assert.False(edits.HasEdits);
        edits.Set(1, 1, false);
        edits.Clear();
        Assert.Empty(edits.Entries());
    }

    [Fact]
    public void Frames_keep_independent_edits()
    {
        var frames = new FramePixelOverrides();
        frames.ForFrame(2).Set(1, 1, true);
        frames.ForFrame(0).Set(0, 0, false);

        Assert.Equal(new[] { 0, 2 }, frames.EditedFrames());
        Assert.True(frames.TryGetFrame(2, out PixelOverrides? second));
        Assert.True(second!.TryGet(1, 1, out bool active));
        Assert.True(active);
        Assert.False(frames.TryGetFrame(1, out _));

        frames.Clear();
        Assert.False(frames.HasEdits);
    }

    private static (int X, int Y, bool Active)[] Sorted(PixelOverrides edits)
    {
        var values = new List<(int X, int Y, bool Active)>();
        foreach ((int x, int y, bool active) in edits.Entries())
        {
            values.Add((x, y, active));
        }

        return values.ToArray();
    }
}
