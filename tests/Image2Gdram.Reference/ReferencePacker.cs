using System.Text;

namespace Image2Gdram.Reference;

/// <summary>
/// Наивная упаковка «по определению» разделов 3 и 4.3 ТЗ. Каждый байт строится как строка из 8 символов
/// '0'/'1', записанная от бита 7 к биту 0, и переводится в число <see cref="Convert.ToByte(string, int)"/>.
/// Скорость не важна, важна очевидная проверяемость. Код ядра не используется (решение N-28).
/// </summary>
public static class ReferencePacker
{
    /// <param name="active">Признак активного пикселя, индексы <c>[x, y]</c>: x — столбец, y — строка.</param>
    public static byte[] Pack(bool[,] active, RefOptions options)
    {
        int width = active.GetLength(0);
        int height = active.GetLength(1);
        if (width < 1 || height < 1)
        {
            throw new ArgumentException("Image must be at least 1x1.", nameof(active));
        }

        return options.Direction == RefDirection.Horizontal
            ? PackHorizontal(active, width, height, options)
            : PackVertical(active, width, height, options);
    }

    // п. 4.3.2: «байт содержит соседние пиксели одной строки; байты идут слева направо, строки — сверху вниз».
    private static byte[] PackHorizontal(bool[,] active, int width, int height, RefOptions options)
    {
        int b = options.BitsPerByte;
        if (b != 8 && b != 6)
        {
            throw new ArgumentException("Bits per byte must be 8 or 6.", nameof(options));
        }

        var output = new List<byte>();
        for (int y = 0; y < height; y++)
        {
            for (int firstX = 0; firstX < width; firstX += b)
            {
                // Пиксели группы слева направо; за правым краем — биты дополнения (фон).
                var group = new StringBuilder();
                for (int k = 0; k < b; k++)
                {
                    int x = firstX + k;
                    group.Append(x < width && active[x, y] ? '1' : '0');
                }

                output.Add(MakeByte(group.ToString(), options));
            }
        }

        return output.ToArray();
    }

    // п. 4.3.2 и 4.3.4: байт — 8 пикселей столбца в пределах страницы; страница N — строки 8N…8N+7.
    private static byte[] PackVertical(bool[,] active, int width, int height, RefOptions options)
    {
        var pages = new List<byte[]>();
        for (int firstY = 0; firstY < height; firstY += 8)
        {
            var pageBytes = new byte[width];
            for (int x = 0; x < width; x++)
            {
                // Пиксели столбца сверху вниз; ниже последней строки — биты дополнения (фон).
                var group = new StringBuilder();
                for (int k = 0; k < 8; k++)
                {
                    int y = firstY + k;
                    group.Append(y < height && active[x, y] ? '1' : '0');
                }

                pageBytes[x] = MakeByte(group.ToString(), options);
            }

            pages.Add(pageBytes);
        }

        var output = new List<byte>();
        if (options.Traversal == RefTraversal.ByPages)
        {
            // Сначала все столбцы страницы 0 слева направо, затем страницы 1 и т. д.
            foreach (byte[] page in pages)
            {
                output.AddRange(page);
            }
        }
        else
        {
            // Для каждого столбца слева направо подряд байты всех его страниц сверху вниз.
            for (int x = 0; x < width; x++)
            {
                foreach (byte[] page in pages)
                {
                    output.Add(page[x]);
                }
            }
        }

        return output.ToArray();
    }

    /// <param name="pixels">Пиксели группы в порядке «первый — левый/верхний», длиной b (8 или 6).</param>
    private static byte MakeByte(string pixels, RefOptions options)
    {
        // MSB first: первый пиксель — старший используемый бит, то есть стоит левее в записи от бита 7 к биту 0.
        // LSB first: первый пиксель — бит 0, то есть последний в записи.
        string used = options.BitOrder == RefBitOrder.MsbFirst ? pixels : Reverse(pixels);

        // Неиспользуемые старшие биты (7 и 6 при 6 битах в байте) — биты дополнения.
        string bits = new string('0', 8 - pixels.Length) + used;

        // Раздел 3 и п. 4.3.7: при инверсии все биты, включая биты дополнения, меняют значение.
        if (options.Invert)
        {
            bits = bits.Replace('0', 'x').Replace('1', '0').Replace('x', '1');
        }

        return Convert.ToByte(bits, 2);
    }

    private static string Reverse(string value)
    {
        char[] chars = value.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}
