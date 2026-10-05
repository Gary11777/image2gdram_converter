namespace Image2Gdram.Core.Editing;

/// <summary>Одно уже выполненное действие, которое можно отменить и повторить.</summary>
public interface IEditAction
{
    void Undo();

    void Redo();
}

/// <summary>
/// История правок пикселей (п. 4.1.5 ТЗ, решение N-14). Глубина — 200 действий.
/// В стек кладётся действие, которое уже применено. Новое действие стирает ветку повтора.
/// Параметры конвейера сюда не входят.
/// </summary>
public sealed class EditHistory
{
    public const int Depth = 200;

    private readonly List<IEditAction> _undo = new();
    private readonly List<IEditAction> _redo = new();

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public int UndoCount => _undo.Count;

    public void Push(IEditAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _redo.Clear();
        _undo.Add(action);
        if (_undo.Count > Depth)
        {
            _undo.RemoveAt(0);
        }
    }

    public void Undo()
    {
        if (_undo.Count == 0)
        {
            throw new InvalidOperationException("Nothing to undo.");
        }

        IEditAction action = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        action.Undo();
        _redo.Add(action);
    }

    public void Redo()
    {
        if (_redo.Count == 0)
        {
            throw new InvalidOperationException("Nothing to redo.");
        }

        IEditAction action = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        action.Redo();
        _undo.Add(action);
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
