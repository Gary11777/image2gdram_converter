using Image2Gdram.Core.Editing;
using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Tests.Editing;

public class EditHistoryTests
{
    [Fact]
    public void One_stroke_is_one_undo_step()
    {
        var edits = new PixelOverrides();
        var history = new EditHistory();
        PixelStrokeAction? stroke = PixelStrokeAction.Apply(edits, new[] { (0, 0), (1, 0), (0, 0) }, active: true);
        Assert.NotNull(stroke);
        Assert.Equal(2, stroke.ChangeCount);
        history.Push(stroke);

        history.Undo();
        Assert.False(edits.HasEdits);
        history.Redo();
        Assert.True(edits.TryGet(0, 0, out bool active) && active);
        Assert.True(edits.TryGet(1, 0, out active) && active);
    }

    [Fact]
    public void Stroke_that_changes_nothing_is_not_recorded()
    {
        var edits = new PixelOverrides();
        edits.Set(0, 0, true);
        Assert.Null(PixelStrokeAction.Apply(edits, new[] { (0, 0) }, active: true));
    }

    [Fact]
    public void New_action_drops_the_redo_branch()
    {
        var history = new EditHistory();
        var log = new List<int>();
        history.Push(new Mark(log, 1));
        history.Push(new Mark(log, 2));
        history.Undo();
        history.Push(new Mark(log, 3));
        log.Clear();

        Assert.False(history.CanRedo);
        history.Undo();
        history.Undo();
        Assert.Equal(new[] { -3, -1 }, log);
    }

    [Fact]
    public void History_keeps_only_the_last_200_actions()
    {
        var history = new EditHistory();
        var log = new List<int>();
        for (int i = 1; i <= EditHistory.Depth + 1; i++)
        {
            history.Push(new Mark(log, i));
        }

        Assert.Equal(EditHistory.Depth, history.UndoCount);
        for (int i = 0; i < EditHistory.Depth; i++)
        {
            history.Undo();
        }

        Assert.False(history.CanUndo);
        Assert.Equal(-201, log[0]);
        Assert.Equal(-2, log[^1]);
        Assert.DoesNotContain(-1, log);
    }

    [Fact]
    public void Undo_restores_the_previous_override()
    {
        var edits = new PixelOverrides();
        var history = new EditHistory();
        history.Push(PixelStrokeAction.Apply(edits, new[] { (3, 4) }, true)!);
        history.Push(PixelStrokeAction.Apply(edits, new[] { (3, 4) }, false)!);

        history.Undo();
        Assert.True(edits.TryGet(3, 4, out bool active) && active);
        history.Undo();
        Assert.False(edits.TryGet(3, 4, out _));
    }

    private sealed class Mark : IEditAction
    {
        private readonly List<int> _log;
        private readonly int _id;

        public Mark(List<int> log, int id)
        {
            _log = log;
            _id = id;
        }

        public void Undo() => _log.Add(-_id);

        public void Redo() => _log.Add(_id);
    }
}
