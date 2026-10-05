using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Processing;

/// <summary>Шаг 5: яркость → монохромный растр (п. 4.1.4 ТЗ).</summary>
public interface IBinarizer
{
    /// <summary>Пиксель активен, когда квантованное значение — 0 (тёмное). Порог 0…255.</summary>
    MonoBitmap Apply(GrayImage gray, int threshold);
}
