using Image2Gdram.Core.Packing;
using Image2Gdram.Reference;

namespace Image2Gdram.Core.Tests.Packing;

internal static class TestBitmaps
{
    /// <summary>Растр из строк: '#' — активный пиксель, '.' — фон.</summary>
    public static MonoBitmap FromRows(params string[] rows)
    {
        var bitmap = new MonoBitmap(rows[0].Length, rows.Length);
        for (int y = 0; y < rows.Length; y++)
        {
            Assert.Equal(bitmap.Width, rows[y].Length);
            for (int x = 0; x < bitmap.Width; x++)
            {
                bitmap[x, y] = rows[y][x] switch
                {
                    '#' => true,
                    '.' => false,
                    _ => throw new ArgumentException($"Unexpected character '{rows[y][x]}'."),
                };
            }
        }

        return bitmap;
    }

    public static MonoBitmap Filled(int width, int height, bool active)
    {
        var bitmap = new MonoBitmap(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bitmap[x, y] = active;
            }
        }

        return bitmap;
    }

    public static MonoBitmap SinglePixel(int width, int height, int x, int y)
    {
        var bitmap = new MonoBitmap(width, height);
        bitmap[x, y] = true;
        return bitmap;
    }

    /// <summary>Случайный растр с фиксированным seed (раздел 2.9 agents.md: детерминизм).</summary>
    public static MonoBitmap Random(int width, int height, int seed)
    {
        var random = new Random(seed);
        var bitmap = new MonoBitmap(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bitmap[x, y] = random.Next(2) == 1;
            }
        }

        return bitmap;
    }

    public static int SeedFor(int width, int height) => 1_000_003 * width + height;

    public static bool[,] ToArray(MonoBitmap bitmap)
    {
        var active = new bool[bitmap.Width, bitmap.Height];
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                active[x, y] = bitmap[x, y];
            }
        }

        return active;
    }

    public static PackingOptions ToCore(RefOptions options) => new()
    {
        Direction = options.Direction == RefDirection.Horizontal ? PackDirection.Horizontal : PackDirection.Vertical,
        BitOrder = options.BitOrder == RefBitOrder.MsbFirst ? BitOrder.MsbFirst : BitOrder.LsbFirst,
        BitsPerByte = options.BitsPerByte,
        PageTraversal = options.Traversal == RefTraversal.ByColumns ? PageTraversal.ByColumns : PageTraversal.ByPages,
        Invert = options.Invert,
    };

    /// <summary>Индексы 0…15 всех допустимых комбинаций <see cref="RefOptions.AllCombinations"/>.</summary>
    public static IEnumerable<int> CombinationIndexes => Enumerable.Range(0, RefOptions.AllCombinations.Count);

    /// <summary>Размеры для свойств упаковки: 1×1, некратные 8 и 6 по обеим осям, ячейки шрифтов, экраны пресетов.</summary>
    public static readonly (int Width, int Height)[] Sizes =
    {
        (1, 1), (1, 9), (9, 1), (5, 3), (6, 8), (8, 8), (12, 16), (13, 11),
        (7, 17), (17, 7), (23, 9), (64, 48), (128, 64), (240, 128),
    };

    public static IEnumerable<object[]> SizeAndCombination() =>
        from size in Sizes
        from combination in CombinationIndexes
        select new object[] { size.Width, size.Height, combination };
}
