using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Editing;

/// <summary>
/// Один штрих мыши: несколько пикселей, одно действие истории (п. 4.1.5 ТЗ).
/// Повтор одной и той же клетки в штрихе учитывается один раз, по первому вхождению.
/// </summary>
public sealed class PixelStrokeAction : IEditAction
{
    private readonly PixelOverrides _target;
    private readonly Change[] _changes;

    private PixelStrokeAction(PixelOverrides target, Change[] changes)
    {
        _target = target;
        _changes = changes;
    }

    public int ChangeCount => _changes.Length;

    /// <summary>
    /// Записывает <paramref name="active"/> в перечисленные клетки и возвращает действие.
    /// Если ни одна клетка не изменилась, возвращает <see langword="null"/> — в историю его не кладут.
    /// </summary>
    public static PixelStrokeAction? Apply(PixelOverrides target, IEnumerable<(int X, int Y)> points, bool active)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(points);

        var seen = new HashSet<long>();
        var changes = new List<Change>();
        foreach ((int x, int y) in points)
        {
            long key = ((long)y << 32) | (uint)x;
            if (!seen.Add(key))
            {
                continue;
            }

            bool had = target.TryGet(x, y, out bool previous);
            if (had && previous == active)
            {
                continue;
            }

            target.Set(x, y, active);
            changes.Add(new Change(x, y, had, previous, active));
        }

        return changes.Count == 0 ? null : new PixelStrokeAction(target, changes.ToArray());
    }

    public void Undo()
    {
        for (int i = _changes.Length - 1; i >= 0; i--)
        {
            Change change = _changes[i];
            if (change.HadPrevious)
            {
                _target.Set(change.X, change.Y, change.Previous);
            }
            else
            {
                _target.Remove(change.X, change.Y);
            }
        }
    }

    public void Redo()
    {
        foreach (Change change in _changes)
        {
            _target.Set(change.X, change.Y, change.Active);
        }
    }

    private readonly record struct Change(int X, int Y, bool HadPrevious, bool Previous, bool Active);
}
