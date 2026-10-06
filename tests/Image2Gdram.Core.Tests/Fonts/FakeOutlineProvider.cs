using Image2Gdram.Core.Fonts;

namespace Image2Gdram.Core.Tests.Fonts;

/// <summary>
/// Провайдер контуров без WPF: у каждого символа, кроме перечисленных в <see cref="Missing"/>, контур —
/// квадрат 2×2 над базовой линией у левого края; пробел — пустой контур. Считает обращения.
/// </summary>
internal sealed class FakeOutlineProvider : IGlyphOutlineProvider
{
    public const string Family = "Fake Mono";

    public FontMetrics Metrics { get; init; } = new(6, 2);

    public HashSet<char> Missing { get; } = new();

    public Dictionary<char, GlyphOutline> Overrides { get; } = new();

    public int MetricsCalls { get; private set; }

    public int OutlineCalls { get; private set; }

    public IReadOnlyList<string> GetInstalledFamilies() => new[] { Family };

    public bool TryGetMetrics(FontFaceSpec face, out FontMetrics metrics)
    {
        MetricsCalls++;
        metrics = Metrics;
        return face.Family == Family;
    }

    public GlyphOutline? GetOutline(FontFaceSpec face, char character)
    {
        OutlineCalls++;
        if (face.Family != Family || Missing.Contains(character))
        {
            return null;
        }

        if (Overrides.TryGetValue(character, out GlyphOutline? outline))
        {
            return outline;
        }

        return character == ' ' ? GlyphOutline.Empty : new GlyphOutline(new[] { Polygons.Rect(0, -2, 2, 0) });
    }
}
