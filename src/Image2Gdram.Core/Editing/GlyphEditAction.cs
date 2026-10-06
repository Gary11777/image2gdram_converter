using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Editing;

/// <summary>
/// Операция над одним символом таблицы шрифта (переключение пикселей, очистка, инверсия, сдвиг, вставка,
/// возврат к источнику) — одно действие истории (решения D-14, N-14). Запоминает ручное переопределение
/// символа до и после операции.
/// </summary>
public sealed class GlyphEditAction : IEditAction
{
    private readonly FontTable _table;
    private readonly MonoBitmap? _before;
    private readonly MonoBitmap? _after;

    private GlyphEditAction(FontTable table, int code, MonoBitmap? before, MonoBitmap? after)
    {
        _table = table;
        Code = code;
        _before = before;
        _after = after;
    }

    public int Code { get; }

    /// <summary>
    /// Делает <paramref name="manual"/> ручным изображением символа (<see langword="null"/> — вернуть к источнику)
    /// и возвращает действие. Если ничего не изменилось, возвращает <see langword="null"/> — в историю его не кладут.
    /// </summary>
    public static GlyphEditAction? Apply(FontTable table, int code, MonoBitmap? manual)
    {
        ArgumentNullException.ThrowIfNull(table);
        MonoBitmap? before = table.GetManualGlyph(code);
        if (before is null ? manual is null : before.ContentEquals(manual))
        {
            return null;
        }

        var action = new GlyphEditAction(table, code, before, manual?.Clone());
        action.Redo();
        return action;
    }

    public void Undo() => Assign(_before);

    public void Redo() => Assign(_after);

    private void Assign(MonoBitmap? manual)
    {
        if (manual is null)
        {
            _table.RevertToSource(Code);
        }
        else
        {
            _table.SetManual(Code, manual);
        }
    }
}
