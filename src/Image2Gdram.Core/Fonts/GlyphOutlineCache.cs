namespace Image2Gdram.Core.Fonts;

/// <summary>
/// Кэш контуров и метрик поверх <see cref="IGlyphOutlineProvider"/> (решение D-15): смена смещения,
/// режима отрисовки, порога, диапазонов и параметров упаковки не обращается к шрифту повторно.
/// Хранит до <see cref="MaxFaces"/> гарнитур; при переполнении удаляется давно не использованная.
/// Потокобезопасен.
/// </summary>
public sealed class GlyphOutlineCache
{
    public const int MaxFaces = 8;

    private readonly object _lock = new();
    private readonly LinkedList<FaceEntry> _faces = new();

    public GlyphOutlineCache(IGlyphOutlineProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        Provider = provider;
    }

    public IGlyphOutlineProvider Provider { get; }

    public bool TryGetMetrics(FontFaceSpec face, out FontMetrics metrics)
    {
        FaceEntry entry = GetEntry(face);
        metrics = entry.Metrics;
        return entry.Installed;
    }

    public GlyphOutline? GetOutline(FontFaceSpec face, char character)
    {
        FaceEntry entry = GetEntry(face);
        if (!entry.Installed)
        {
            return null;
        }

        lock (entry.Outlines)
        {
            if (!entry.Outlines.TryGetValue(character, out GlyphOutline? outline))
            {
                outline = Provider.GetOutline(face, character);
                entry.Outlines.Add(character, outline);
            }

            return outline;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _faces.Clear();
        }
    }

    private FaceEntry GetEntry(FontFaceSpec face)
    {
        ArgumentNullException.ThrowIfNull(face);
        lock (_lock)
        {
            for (LinkedListNode<FaceEntry>? node = _faces.First; node is not null; node = node.Next)
            {
                if (node.Value.Face == face)
                {
                    _faces.Remove(node);
                    _faces.AddFirst(node);
                    return node.Value;
                }
            }

            bool installed = Provider.TryGetMetrics(face, out FontMetrics metrics);
            var entry = new FaceEntry(face, installed, metrics);
            _faces.AddFirst(entry);
            if (_faces.Count > MaxFaces)
            {
                _faces.RemoveLast();
            }

            return entry;
        }
    }

    private sealed class FaceEntry(FontFaceSpec face, bool installed, FontMetrics metrics)
    {
        public FontFaceSpec Face { get; } = face;

        public bool Installed { get; } = installed;

        public FontMetrics Metrics { get; } = metrics;

        public Dictionary<char, GlyphOutline?> Outlines { get; } = new();
    }
}
