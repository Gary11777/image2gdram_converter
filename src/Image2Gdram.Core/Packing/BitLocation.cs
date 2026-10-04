namespace Image2Gdram.Core.Packing;

/// <summary>Положение пикселя в упакованном массиве: индекс байта и номер бита (0 — младший).</summary>
public readonly record struct BitLocation(int ByteIndex, int Bit);
