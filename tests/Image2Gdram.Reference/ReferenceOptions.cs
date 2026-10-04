namespace Image2Gdram.Reference;

public enum RefDirection
{
    Horizontal,
    Vertical,
}

public enum RefBitOrder
{
    LsbFirst,
    MsbFirst,
}

public enum RefTraversal
{
    ByPages,
    ByColumns,
}

/// <summary>
/// Комбинация параметров упаковки для эталона. Собственные типы, чтобы не зависеть от ядра (решение N-28).
/// В горизонтальном режиме <see cref="Traversal"/> не применяется, в вертикальном — <see cref="BitsPerByte"/> всегда 8.
/// </summary>
public sealed record RefOptions(
    RefDirection Direction,
    RefBitOrder BitOrder,
    int BitsPerByte,
    RefTraversal Traversal,
    bool Invert)
{
    /// <summary>
    /// Все 16 допустимых комбинаций раздела 11 ТЗ в фиксированном порядке:
    /// горизонтальный режим — порядок бит × бит в байте × инверсия (8),
    /// вертикальный — порядок бит × порядок обхода × инверсия (8).
    /// </summary>
    public static IReadOnlyList<RefOptions> AllCombinations { get; } = BuildAll();

    /// <summary>Короткое имя для файлов эталонов, например <c>h_msb_6_inv</c> или <c>v_lsb_cols</c>.</summary>
    public string Name
    {
        get
        {
            string order = BitOrder == RefBitOrder.MsbFirst ? "msb" : "lsb";
            string body = Direction == RefDirection.Horizontal
                ? $"h_{order}_{BitsPerByte}"
                : $"v_{order}_{(Traversal == RefTraversal.ByColumns ? "cols" : "pages")}";
            return Invert ? body + "_inv" : body;
        }
    }

    private static RefOptions[] BuildAll()
    {
        var all = new List<RefOptions>();
        foreach (RefBitOrder order in new[] { RefBitOrder.LsbFirst, RefBitOrder.MsbFirst })
        {
            foreach (int bits in new[] { 8, 6 })
            {
                foreach (bool invert in new[] { false, true })
                {
                    all.Add(new RefOptions(RefDirection.Horizontal, order, bits, RefTraversal.ByPages, invert));
                }
            }
        }

        foreach (RefBitOrder order in new[] { RefBitOrder.LsbFirst, RefBitOrder.MsbFirst })
        {
            foreach (RefTraversal traversal in new[] { RefTraversal.ByPages, RefTraversal.ByColumns })
            {
                foreach (bool invert in new[] { false, true })
                {
                    all.Add(new RefOptions(RefDirection.Vertical, order, 8, traversal, invert));
                }
            }
        }

        return all.ToArray();
    }
}
