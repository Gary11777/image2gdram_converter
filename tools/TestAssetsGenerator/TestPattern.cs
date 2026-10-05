namespace Image2Gdram.TestAssets;

/// <summary>
/// «Таблица настройки» приложения Б и спрайт 13×11.
/// Координаты элементов — решение N-40: в ТЗ заданы состав и отступ маркеров, но не место остальных фигур.
/// </summary>
public static class TestPattern
{
    public const int FrameThickness = 1;
    public const int MarkerSize = 8;
    public const int MarkerGap = 2;

    /// <summary>Левый верхний угол углового маркера: рамка 1 пиксель и отступ 2 пикселя.</summary>
    public const int MarkerOrigin = FrameThickness + MarkerGap;

    public const int BlockOrigin = 16;
    public const int BlockSize = 16;
    public const int BlockGap = 4;
    public const int LabelX = 76;
    public const int LabelY = 20;

    public static int MarkerRight(int width) => width - FrameThickness - MarkerGap - MarkerSize;

    public static int MarkerBottom(int height) => height - FrameThickness - MarkerGap - MarkerSize;

    public static PatternBitmap CreateScreen(int width, int height)
    {
        if (!((width == 240 && height == 128) || (width == 128 && height == 64)))
        {
            throw new ArgumentException("Supported screens are 240x128 and 128x64.", nameof(width));
        }

        string label = $"{width}x{height}";
        var image = new PatternBitmap(width, height, label);
        DrawBlock(image, BlockOrigin, BlockOrigin, PatternElement.Checker, (x, y) => ((x + y) & 1) == 0);
        int filledLeft = BlockOrigin + BlockSize + BlockGap;
        DrawBlock(image, filledLeft, BlockOrigin, PatternElement.FilledRectangle, (_, _) => true);
        int emptyLeft = filledLeft + BlockSize + BlockGap;
        int emptyBottom = BlockOrigin + BlockSize - 1;
        int emptyRight = emptyLeft + BlockSize - 1;
        DrawBlock(
            image,
            emptyLeft,
            BlockOrigin,
            PatternElement.EmptyRectangle,
            (x, y) => x == emptyLeft || y == BlockOrigin || x == emptyRight || y == emptyBottom);
        DrawDiagonals(image);
        DrawLabel(image, label);
        DrawMarkers(image);
        DrawFrame(image);
        DrawTicks(image);
        return image;
    }

    public static PatternBitmap CreateSprite()
    {
        string[] rows =
        [
            "###..........",
            "#............",
            "#..###.......",
            "#............",
            "#......#.....",
            ".......#.....",
            ".......###...",
            ".............",
            "...........#.",
            "..........##.",
            ".........###.",
        ];

        var image = new PatternBitmap(13, 11, label: null);
        for (int y = 0; y < rows.Length; y++)
        {
            for (int x = 0; x < rows[y].Length; x++)
            {
                if (rows[y][x] == '#')
                {
                    image.Claim(x, y, PatternElement.Sprite, active: true);
                }
            }
        }

        return image;
    }

    private static void DrawBlock(PatternBitmap image, int left, int top, PatternElement element, Func<int, int, bool> active)
    {
        for (int y = top; y < top + BlockSize; y++)
        {
            for (int x = left; x < left + BlockSize; x++)
            {
                image.Claim(x, y, element, active(x, y));
            }
        }
    }

    private static void DrawDiagonals(PatternBitmap image)
    {
        if (image.Height == 64)
        {
            DrawDiagonal(image, 16, 36, 16, downRight: true);
            DrawDiagonal(image, 40, 36, 16, downRight: false);
            return;
        }

        DrawDiagonal(image, 16, 48, 32, downRight: true);
        DrawDiagonal(image, 64, 48, 32, downRight: false);
    }

    private static void DrawDiagonal(PatternBitmap image, int x, int y, int length, bool downRight)
    {
        for (int i = 0; i < length; i++)
        {
            int px = downRight ? x + i : x + length - 1 - i;
            image.Claim(px, y + i, PatternElement.Diagonal, active: true);
        }
    }

    private static void DrawLabel(PatternBitmap image, string label)
    {
        int width = Font5x7.Measure(label);
        for (int y = 0; y < Font5x7.Height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image.Claim(LabelX + x, LabelY + y, PatternElement.Label, active: false);
            }
        }

        int cursor = LabelX;
        foreach (char glyph in label)
        {
            for (int y = 0; y < Font5x7.Height; y++)
            {
                for (int x = 0; x < Font5x7.Width; x++)
                {
                    if (Font5x7.IsInk(glyph, x, y))
                    {
                        image.Claim(cursor + x, LabelY + y, PatternElement.Label, active: true);
                    }
                }
            }

            cursor += Font5x7.Width + 1;
        }
    }

    private static void DrawMarkers(PatternBitmap image)
    {
        int left = MarkerOrigin;
        int right = MarkerRight(image.Width);
        int top = MarkerOrigin;
        int bottom = MarkerBottom(image.Height);
        DrawMarker(image, left, top, PatternElement.MarkerSolid, static (x, y) => true);
        DrawMarker(image, right, top, PatternElement.MarkerOutline, static (x, y) => x == 0 || y == 0 || x == MarkerSize - 1 || y == MarkerSize - 1);
        DrawMarker(image, left, bottom, PatternElement.MarkerCross, static (x, y) => x == y || x + y == MarkerSize - 1);
        DrawMarker(image, right, bottom, PatternElement.MarkerDiagonal, static (x, y) => x == y);
    }

    private static void DrawMarker(PatternBitmap image, int left, int top, PatternElement element, Func<int, int, bool> active)
    {
        for (int y = 0; y < MarkerSize; y++)
        {
            for (int x = 0; x < MarkerSize; x++)
            {
                image.Claim(left + x, top + y, element, active(x, y));
            }
        }
    }

    private static void DrawFrame(PatternBitmap image)
    {
        for (int x = 0; x < image.Width; x++)
        {
            image.Claim(x, 0, PatternElement.Frame, active: true);
            image.Claim(x, image.Height - 1, PatternElement.Frame, active: true);
        }

        for (int y = 1; y < image.Height - 1; y++)
        {
            image.Claim(0, y, PatternElement.Frame, active: true);
            image.Claim(image.Width - 1, y, PatternElement.Frame, active: true);
        }
    }

    private static void DrawTicks(PatternBitmap image)
    {
        int number = 1;
        for (int x = 8; x < image.Width; x += 8, number++)
        {
            DrawTick(image, x, 0, vertical: true, TickLength(number));
        }

        number = 1;
        for (int y = 8; y < image.Height; y += 8, number++)
        {
            DrawTick(image, 0, y, vertical: false, TickLength(number));
        }
    }

    /// <summary>Длина метки с номером с единицы. Каждая восьмая — 6 пикселей, остальные — 3.</summary>
    public static int TickLength(int numberFromOne) => numberFromOne % 8 == 0 ? 6 : 3;

    private static void DrawTick(PatternBitmap image, int x, int y, bool vertical, int length)
    {
        for (int i = 0; i < length; i++)
        {
            int px = vertical ? x : x + i;
            int py = vertical ? y + i : y;
            if (image.OwnerAt(px, py) == PatternElement.Frame)
            {
                continue;
            }

            image.Claim(px, py, PatternElement.Tick, active: true);
        }
    }
}
