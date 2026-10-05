using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Processing;

/// <summary>
/// Разреженные ручные правки одного кадра поверх бинаризации (шаг 6, решение F-11).
/// Перечисление идёт по возрастанию y, затем x — так сериализация детерминирована.
/// Координаты вне растра при наложении пропускаются.
/// </summary>
public sealed class PixelOverrides
{
    private readonly Dictionary<long, bool> _pixels = new();

    public int Count => _pixels.Count;

    public bool HasEdits => _pixels.Count > 0;

    public void Set(int x, int y, bool active) => _pixels[Key(x, y)] = active;

    public bool Remove(int x, int y) => _pixels.Remove(Key(x, y));

    public void Clear() => _pixels.Clear();

    public bool TryGet(int x, int y, out bool active) => _pixels.TryGetValue(Key(x, y), out active);

    public void Apply(MonoBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        foreach ((int x, int y, bool active) in Entries())
        {
            if ((uint)x < (uint)bitmap.Width && (uint)y < (uint)bitmap.Height)
            {
                bitmap[x, y] = active;
            }
        }
    }

    public IEnumerable<(int X, int Y, bool Active)> Entries()
    {
        if (_pixels.Count == 0)
        {
            yield break;
        }

        var keys = new long[_pixels.Count];
        _pixels.Keys.CopyTo(keys, 0);
        Array.Sort(keys);
        foreach (long key in keys)
        {
            yield return (X(key), Y(key), _pixels[key]);
        }
    }

    private static long Key(int x, int y) => ((long)y << 32) | (uint)x;

    private static int X(long key) => (int)(uint)key;

    private static int Y(long key) => (int)(key >> 32);
}
