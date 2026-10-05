namespace Image2Gdram.TestAssets;

/// <summary>Элемент тестового изображения. Нужен, чтобы проверить, что фигуры не наезжают друг на друга.</summary>
public enum PatternElement
{
    None = 0,
    Frame = 1,
    MarkerSolid = 2,
    MarkerOutline = 3,
    MarkerCross = 4,
    MarkerDiagonal = 5,
    Tick = 6,
    Diagonal = 7,
    Checker = 8,
    FilledRectangle = 9,
    EmptyRectangle = 10,
    Label = 11,
    Sprite = 12,
}

/// <summary>Монохромный растр тестового изображения: активный пиксель — чёрный.</summary>
public sealed class PatternBitmap
{
    private readonly bool[] _active;
    private readonly byte[] _owner;

    internal PatternBitmap(int width, int height, string? label)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        Width = width;
        Height = height;
        Label = label;
        _active = new bool[width * height];
        _owner = new byte[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public string? Label { get; }

    public bool IsActive(int x, int y) => _active[Index(x, y)];

    public PatternElement OwnerAt(int x, int y) => (PatternElement)_owner[Index(x, y)];

    internal void Claim(int x, int y, PatternElement element, bool active)
    {
        if (element == PatternElement.None)
        {
            throw new ArgumentOutOfRangeException(nameof(element));
        }

        int index = Index(x, y);
        var current = (PatternElement)_owner[index];
        if (current != PatternElement.None && current != element)
        {
            throw new InvalidOperationException($"Overlap at ({x}, {y}): {current} and {element}.");
        }

        _owner[index] = (byte)element;
        if (active)
        {
            _active[index] = true;
        }
    }

    private int Index(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"Point ({x}, {y}) is outside {Width}x{Height}.");
        }

        return y * Width + x;
    }
}
