using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Packing;

namespace image2gdram_converter.Services;

/// <summary>Выбор массива и параметров раскладки в диалоге импорта (решение N-21).</summary>
public sealed class ImportPick
{
    public ImportPick(int arrayIndex, FontCellSize cell, PackingOptions packing)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(packing);
        ArrayIndex = arrayIndex;
        Cell = cell;
        Packing = packing;
    }

    public int ArrayIndex { get; }

    public FontCellSize Cell { get; }

    public PackingOptions Packing { get; }
}
