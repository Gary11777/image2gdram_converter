namespace Image2Gdram.TestAssets;

/// <summary>
/// Встроенный растр 5x7 для листов шрифтов (решение N-60). Не системный шрифт.
/// Код без рисунка получает метку из младших бит кода, чтобы ячейки отличались друг от друга.
/// Код 0 меток не имеет и остаётся пустым.
/// </summary>
public static class Glyph5x7
{
    public const int Width = 5;
    public const int Height = 7;

    private static readonly Dictionary<int, string[]> Glyphs = Build();

    public static bool IsInk(int code, int x, int y)
    {
        if ((uint)code > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(code));
        }

        if ((uint)x >= Width || (uint)y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (Glyphs.TryGetValue(code, out string[]? rows))
        {
            return rows[y][x] == 'X';
        }

        int bit = y * Width + x;
        return bit < 8 && (code & (1 << bit)) != 0;
    }

    private static Dictionary<int, string[]> Build()
    {
        var glyphs = new Dictionary<int, string[]>();

        void Add(int code, string packed)
        {
            string[] rows = packed.Split('|');
            if (rows.Length != Height || rows.Any(row => row.Length != Width || row.Any(ch => ch is not ('.' or 'X'))))
            {
                throw new InvalidOperationException($"Glyph 0x{code:X2} must be 7 rows of 5 marks '.' or 'X'.");
            }

            glyphs.Add(code, rows);
        }

        void Copy(int code, int source) => glyphs.Add(code, glyphs[source]);

        Add(0x20, ".....|.....|.....|.....|.....|.....|.....");
        Add(0x21, "..X..|..X..|..X..|..X..|..X..|.....|..X..");
        Add(0x22, ".X.X.|.X.X.|.....|.....|.....|.....|.....");
        Add(0x23, ".X.X.|XXXXX|.X.X.|.X.X.|XXXXX|.X.X.|.....");
        Add(0x24, "..X..|.XXXX|X.X..|.XXX.|..X.X|XXXX.|.X...");
        Add(0x25, "X...X|X..X.|...X.|..X..|.X...|X..X.|X...X");
        Add(0x26, ".XX..|X..X.|.XX..|X.X..|X..X.|.XX.X|.....");
        Add(0x27, "..X..|..X..|.....|.....|.....|.....|.....");
        Add(0x28, "...X.|..X..|.X...|.X...|.X...|..X..|...X.");
        Add(0x29, ".X...|..X..|...X.|...X.|...X.|..X..|.X...");
        Add(0x2A, ".....|.X.X.|..X..|XXXXX|..X..|.X.X.|.....");
        Add(0x2B, ".....|..X..|..X..|XXXXX|..X..|..X..|.....");
        Add(0x2C, ".....|.....|.....|..X..|..X..|.X...|.....");
        Add(0x2D, ".....|.....|.....|XXXXX|.....|.....|.....");
        Add(0x2E, ".....|.....|.....|.....|.....|..X..|..X..");
        Add(0x2F, "....X|....X|...X.|..X..|.X...|X....|X....");

        Add(0x30, ".XXX.|X...X|X..XX|X.X.X|XX..X|X...X|.XXX.");
        Add(0x31, "..X..|.XX..|..X..|..X..|..X..|..X..|.XXX.");
        Add(0x32, ".XXX.|X...X|....X|...X.|..X..|.X...|XXXXX");
        Add(0x33, ".XXX.|X...X|....X|..XX.|....X|X...X|.XXX.");
        Add(0x34, "...X.|..XX.|.X.X.|X..X.|XXXXX|...X.|...X.");
        Add(0x35, "XXXXX|X....|XXXX.|....X|....X|X...X|.XXX.");
        Add(0x36, ".XXX.|X....|X....|XXXX.|X...X|X...X|.XXX.");
        Add(0x37, "XXXXX|....X|...X.|..X..|.X...|.X...|.X...");
        Add(0x38, ".XXX.|X...X|X...X|.XXX.|X...X|X...X|.XXX.");
        Add(0x39, ".XXX.|X...X|X...X|.XXXX|....X|....X|.XXX.");

        Add(0x3A, ".....|..X..|..X..|.....|..X..|..X..|.....");
        Add(0x3B, ".....|..X..|..X..|.....|..X..|.X...|.....");
        Add(0x3C, "...X.|..X..|.X...|X....|.X...|..X..|...X.");
        Add(0x3D, ".....|.....|XXXXX|.....|XXXXX|.....|.....");
        Add(0x3E, ".X...|..X..|...X.|....X|...X.|..X..|.X...");
        Add(0x3F, ".XXX.|X...X|....X|...X.|..X..|.....|..X..");
        Add(0x40, ".XXX.|X...X|X.XXX|X.X.X|X.XXX|X....|.XXX.");

        Add(0x41, ".XXX.|X...X|X...X|XXXXX|X...X|X...X|X...X");
        Add(0x42, "XXXX.|X...X|X...X|XXXX.|X...X|X...X|XXXX.");
        Add(0x43, ".XXX.|X...X|X....|X....|X....|X...X|.XXX.");
        Add(0x44, "XXXX.|X...X|X...X|X...X|X...X|X...X|XXXX.");
        Add(0x45, "XXXXX|X....|X....|XXXX.|X....|X....|XXXXX");
        Add(0x46, "XXXXX|X....|X....|XXXX.|X....|X....|X....");
        Add(0x47, ".XXX.|X...X|X....|X.XXX|X...X|X...X|.XXX.");
        Add(0x48, "X...X|X...X|X...X|XXXXX|X...X|X...X|X...X");
        Add(0x49, "XXXXX|..X..|..X..|..X..|..X..|..X..|XXXXX");
        Add(0x4A, "..XXX|...X.|...X.|...X.|X..X.|X..X.|.XX..");
        Add(0x4B, "X...X|X..X.|X.X..|XX...|X.X..|X..X.|X...X");
        Add(0x4C, "X....|X....|X....|X....|X....|X....|XXXXX");
        Add(0x4D, "X...X|XX.XX|X.X.X|X...X|X...X|X...X|X...X");
        Add(0x4E, "X...X|XX..X|X.X.X|X..XX|X...X|X...X|X...X");
        Add(0x4F, ".XXX.|X...X|X...X|X...X|X...X|X...X|.XXX.");
        Add(0x50, "XXXX.|X...X|X...X|XXXX.|X....|X....|X....");
        Add(0x51, ".XXX.|X...X|X...X|X...X|X.X.X|X..X.|.XX.X");
        Add(0x52, "XXXX.|X...X|X...X|XXXX.|X.X..|X..X.|X...X");
        Add(0x53, ".XXXX|X....|X....|.XXX.|....X|....X|XXXX.");
        Add(0x54, "XXXXX|..X..|..X..|..X..|..X..|..X..|..X..");
        Add(0x55, "X...X|X...X|X...X|X...X|X...X|X...X|.XXX.");
        Add(0x56, "X...X|X...X|X...X|X...X|X...X|.X.X.|..X..");
        Add(0x57, "X...X|X...X|X...X|X.X.X|X.X.X|XX.XX|X...X");
        Add(0x58, "X...X|X...X|.X.X.|..X..|.X.X.|X...X|X...X");
        Add(0x59, "X...X|X...X|.X.X.|..X..|..X..|..X..|..X..");
        Add(0x5A, "XXXXX|....X|...X.|..X..|.X...|X....|XXXXX");

        Add(0x5B, ".XXX.|..X..|..X..|..X..|..X..|..X..|.XXX.");
        Add(0x5C, "X....|X....|.X...|..X..|...X.|....X|....X");
        Add(0x5D, ".XXX.|...X.|...X.|...X.|...X.|...X.|.XXX.");
        Add(0x5E, "..X..|.X.X.|X...X|.....|.....|.....|.....");
        Add(0x5F, ".....|.....|.....|.....|.....|.....|XXXXX");
        Add(0x60, "..X..|...X.|.....|.....|.....|.....|.....");

        Add(0x61, ".....|.....|.XXX.|....X|.XXXX|X...X|.XXXX");
        Add(0x62, "X....|X....|XXXX.|X...X|X...X|X...X|XXXX.");
        Add(0x63, ".....|.....|.XXX.|X....|X....|X....|.XXX.");
        Add(0x64, "....X|....X|.XXXX|X...X|X...X|X...X|.XXXX");
        Add(0x65, ".....|.....|.XXX.|X...X|XXXXX|X....|.XXX.");
        Add(0x66, "..XX.|.X...|XXXX.|.X...|.X...|.X...|.X...");
        Add(0x67, ".....|.....|.XXXX|X...X|.XXXX|....X|.XXX.");
        Add(0x68, "X....|X....|XXXX.|X...X|X...X|X...X|X...X");
        Add(0x69, "..X..|.....|..X..|..X..|..X..|..X..|..X..");
        Add(0x6A, "...X.|.....|...X.|...X.|...X.|X..X.|.XX..");
        Add(0x6B, "X....|X....|X..X.|X.X..|XX...|X.X..|X..X.");
        Add(0x6C, "..X..|..X..|..X..|..X..|..X..|..X..|..X..");
        Add(0x6D, ".....|.....|XX.X.|X.X.X|X.X.X|X...X|X...X");
        Add(0x6E, ".....|.....|XXXX.|X...X|X...X|X...X|X...X");
        Add(0x6F, ".....|.....|.XXX.|X...X|X...X|X...X|.XXX.");
        Add(0x70, ".....|.....|XXXX.|X...X|XXXX.|X....|X....");
        Add(0x71, ".....|.....|.XXXX|X...X|.XXXX|....X|....X");
        Add(0x72, ".....|.....|X.XX.|XX...|X....|X....|X....");
        Add(0x73, ".....|.....|.XXXX|X....|.XXX.|....X|XXXX.");
        Add(0x74, ".X...|.X...|XXXX.|.X...|.X...|.X..X|..XX.");
        Add(0x75, ".....|.....|X...X|X...X|X...X|X...X|.XXXX");
        Add(0x76, ".....|.....|X...X|X...X|X...X|.X.X.|..X..");
        Add(0x77, ".....|.....|X...X|X...X|X.X.X|X.X.X|.X.X.");
        Add(0x78, ".....|.....|X...X|.X.X.|..X..|.X.X.|X...X");
        Add(0x79, ".....|.....|X...X|X...X|.XXXX|....X|.XXX.");
        Add(0x7A, ".....|.....|XXXXX|...X.|..X..|.X...|XXXXX");

        Add(0x7B, "..XX.|.X...|.X...|X....|.X...|.X...|..XX.");
        Add(0x7C, "..X..|..X..|..X..|..X..|..X..|..X..|..X..");
        Add(0x7D, ".XX..|...X.|...X.|....X|...X.|...X.|.XX..");
        Add(0x7E, ".....|.XX.X|X.XX.|.....|.....|.....|.....");

        Add(0xA8, ".X.X.|XXXXX|X....|XXXX.|X....|X....|XXXXX");
        Add(0xB8, ".X.X.|.....|.XXX.|X...X|XXXXX|X....|.XXX.");

        Copy(0xC0, 0x41);
        Add(0xC1, "XXXX.|X....|X....|XXXX.|X...X|X...X|XXXX.");
        Copy(0xC2, 0x42);
        Add(0xC3, "XXXXX|X....|X....|X....|X....|X....|X....");
        Add(0xC4, "..XX.|.X..X|.X..X|.X..X|.X..X|XXXXX|X...X");
        Copy(0xC5, 0x45);
        Add(0xC6, "X.X.X|X.X.X|.X.X.|XXXXX|.X.X.|X.X.X|X.X.X");
        Add(0xC7, ".XXX.|X...X|....X|..XX.|....X|X...X|.XXX.");
        Add(0xC8, "X...X|X...X|X..XX|X.X.X|XX..X|X...X|X...X");
        Add(0xC9, ".X.X.|X...X|X..XX|X.X.X|XX..X|X...X|X...X");
        Copy(0xCA, 0x4B);
        Add(0xCB, "..X.X|.X..X|.X..X|X...X|X...X|X...X|X...X");
        Copy(0xCC, 0x4D);
        Copy(0xCD, 0x48);
        Copy(0xCE, 0x4F);
        Add(0xCF, "XXXXX|X...X|X...X|X...X|X...X|X...X|X...X");
        Copy(0xD0, 0x50);
        Copy(0xD1, 0x43);
        Copy(0xD2, 0x54);
        Add(0xD3, "X...X|X...X|.X.X.|..X..|..X..|.X...|X....");
        Add(0xD4, "..X..|XXXXX|X.X.X|X.X.X|XXXXX|..X..|..X..");
        Copy(0xD5, 0x58);
        Add(0xD6, "X...X|X...X|X...X|X...X|X...X|XXXXX|....X");
        Add(0xD7, "X...X|X...X|X...X|.XXXX|....X|....X|....X");
        Add(0xD8, "X.X.X|X.X.X|X.X.X|X.X.X|X.X.X|X.X.X|XXXXX");
        Add(0xD9, "X.X.X|X.X.X|X.X.X|X.X.X|X.X.X|XXXXX|....X");
        Add(0xDA, "XX...|.X...|.X...|.XXX.|.X..X|.X..X|.XXX.");
        Add(0xDB, "X..X.|X..X.|X..X.|XXXX.|X..X.|X..X.|XXXX.");
        Add(0xDC, "X....|X....|X....|XXXX.|X...X|X...X|XXXX.");
        Add(0xDD, ".XXXX|....X|....X|.XXXX|....X|....X|.XXXX");
        Add(0xDE, "X.XX.|X.X.X|X.X.X|XX.XX|X.X.X|X.X.X|X.XX.");
        Add(0xDF, ".XXXX|X...X|X...X|.XXXX|.X..X|X...X|X...X");

        for (int code = 0xE0; code <= 0xFF; code++)
        {
            Copy(code, code - 0x20);
        }

        return glyphs;
    }
}
