namespace Image2Gdram.Core.Processing;

/// <summary>
/// Предел смещения в интерфейсе: не больше 80 % стороны, округление вниз (решение N-67).
/// Абсолютные границы API (±1024 у картинки и ±32 у глифа) этот класс не заменяет.
/// </summary>
public static class OffsetLimits
{
    /// <summary>Доля стороны в процентах.</summary>
    public const int Percent = 80;

    /// <summary>Наибольший модуль смещения для стороны <paramref name="side"/>: <c>side * 80 / 100</c>.</summary>
    public static int For(int side)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(side, 1);
        return side * Percent / 100;
    }

    /// <summary>Ограничивает смещение диапазоном <c>±min(For(side), absoluteMax)</c>.</summary>
    public static int Clamp(int offset, int side, int absoluteMax)
    {
        int limit = Math.Min(For(side), absoluteMax);
        return Math.Clamp(offset, -limit, limit);
    }
}
