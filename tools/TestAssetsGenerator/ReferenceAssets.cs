using System.Globalization;
using System.Text;
using Image2Gdram.Reference;

namespace Image2Gdram.TestAssets;

/// <summary>
/// Пишет эталонные массивы раздела 11 ТЗ независимой упаковкой <see cref="ReferencePacker"/>.
/// Код ядра не вызывается (решения N-28, N-60).
/// </summary>
public static class ReferenceAssets
{
    public static void Write(string testdataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testdataDirectory);
        string root = Path.Combine(testdataDirectory, "reference");
        Directory.CreateDirectory(root);

        var written = new List<WrittenInput>();
        written.Add(WriteImage(root, "test_pattern_240x128", TestPattern.CreateScreen(240, 128)));
        written.Add(WriteImage(root, "test_pattern_128x64", TestPattern.CreateScreen(128, 64)));
        written.Add(WriteImage(root, "test_sprite_13x11", TestPattern.CreateSprite()));
        foreach (FontSheet sheet in FontSheet.All)
        {
            written.Add(WriteFont(root, sheet));
        }

        File.WriteAllText(Path.Combine(root, "index.md"), Index(written), Utf8);
    }

    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static WrittenInput WriteImage(string root, string id, PatternBitmap image)
    {
        var active = new bool[image.Width, image.Height];
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                active[x, y] = image.IsActive(x, y);
            }
        }

        return WriteCombinations(root, id, image.Width, image.Height, "изображение " + id + ".png", options => ReferencePacker.Pack(active, options));
    }

    private static WrittenInput WriteFont(string root, FontSheet sheet)
    {
        var cells = new bool[256][,];
        for (int code = 0; code < 256; code++)
        {
            var cell = new bool[sheet.CellWidth, sheet.CellHeight];
            for (int y = 0; y < sheet.CellHeight; y++)
            {
                for (int x = 0; x < sheet.CellWidth; x++)
                {
                    cell[x, y] = sheet.IsCellInk(code, x, y);
                }
            }

            cells[code] = cell;
        }

        return WriteCombinations(
            root,
            sheet.ReferenceId,
            sheet.CellWidth,
            sheet.CellHeight,
            "шрифт " + sheet.FileName + ", 256 символов подряд",
            options => PackFont(cells, options));
    }

    private static byte[] PackFont(bool[][,] cells, RefOptions options)
    {
        var parts = new List<byte[]>(cells.Length);
        int perChar = 0;
        for (int code = 0; code < cells.Length; code++)
        {
            byte[] packed = ReferencePacker.Pack(cells[code], options);
            perChar = packed.Length;
            parts.Add(packed);
        }

        var all = new byte[perChar * cells.Length];
        for (int code = 0; code < parts.Count; code++)
        {
            parts[code].CopyTo(all, code * perChar);
        }

        return all;
    }

    private static WrittenInput WriteCombinations(
        string root,
        string id,
        int width,
        int height,
        string description,
        Func<RefOptions, byte[]> pack)
    {
        string directory = Path.Combine(root, id);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        Directory.CreateDirectory(directory);
        var files = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (RefOptions options in RefOptions.AllCombinations)
        {
            if (!seen.Add(options.Name))
            {
                throw new InvalidOperationException("Duplicate reference name " + options.Name);
            }

            byte[] bytes = pack(options);
            string stem = id + "_" + options.Name;
            File.WriteAllBytes(Path.Combine(directory, options.Name + ".bin"), bytes);
            File.WriteAllText(Path.Combine(directory, options.Name + ".c"), CSource(stem, id, width, height, options, bytes), Utf8);
            files.Add(options.Name);
        }

        return new WrittenInput(id, description, width, height, files);
    }

    private static string CSource(string arrayName, string id, int width, int height, RefOptions options, byte[] bytes)
    {
        var text = new StringBuilder();
        text.Append("/* Эталон. Независимая упаковка ReferencePacker, не программа Image2GDRAM Converter.\r\n");
        text.Append(" * Вход: ");
        text.Append(id);
        text.Append("\r\n * Упаковка: ");
        text.Append(Describe(options));
        text.Append("\r\n * Размер: ");
        text.Append(width.ToString(CultureInfo.InvariantCulture));
        text.Append('x');
        text.Append(height.ToString(CultureInfo.InvariantCulture));
        text.Append("\r\n * Байт: ");
        text.Append(bytes.Length.ToString(CultureInfo.InvariantCulture));
        text.Append("\r\n */\r\nunsigned char ");
        text.Append(arrayName);
        text.Append('[');
        text.Append(bytes.Length.ToString(CultureInfo.InvariantCulture));
        text.Append("] = {\r\n");
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i % 16 == 0)
            {
                text.Append("    ");
            }

            text.Append("0x");
            text.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));
            if (i != bytes.Length - 1)
            {
                text.Append(',');
                text.Append(i % 16 == 15 ? "\r\n" : " ");
            }
        }

        text.Append("\r\n};\r\n");
        return text.ToString();
    }

    private static string Describe(RefOptions options)
    {
        string direction = options.Direction == RefDirection.Horizontal ? "горизонтальное" : "вертикальное";
        string order = options.BitOrder == RefBitOrder.MsbFirst ? "MSB first" : "LSB first";
        string extra = options.Direction == RefDirection.Horizontal
            ? options.BitsPerByte.ToString(CultureInfo.InvariantCulture) + " бит в байте, обход не применяется"
            : "8 бит в байте, " + (options.Traversal == RefTraversal.ByColumns ? "по столбцам" : "по страницам");
        return direction + ", " + order + ", " + extra + ", инверсия: " + (options.Invert ? "да" : "нет");
    }

    private static string Index(IReadOnlyList<WrittenInput> inputs)
    {
        var text = new StringBuilder();
        text.Append("# Эталонные массивы\r\n\r\n");
        text.Append("Эталоны построены утилитой `tools/TestAssetsGenerator` и независимой упаковкой ");
        text.Append("`tests/Image2Gdram.Reference` (`ReferencePacker`). Код ядра `Image2Gdram.Core` не используется ");
        text.Append("(решения N-28 и N-60). Файлы нужно согласовать с заказчиком (вопрос Q-08). ");
        text.Append("Тестовые изображения приложения Б — вопрос Q-11.\r\n\r\n");
        text.Append("Для каждого входа записаны все 16 допустимых комбинаций упаковки. ");
        text.Append("Имя файла — имя комбинации. Рядом лежат `.bin` (только байты массива) и `.c` ");
        text.Append("(те же байты в инициализаторе C, для чтения; это не вывод программы).\r\n\r\n");
        text.Append("## Входы\r\n\r\n");
        text.Append("| Папка | Что упаковано | Размер |\r\n");
        text.Append("|---|---|---|\r\n");
        foreach (WrittenInput input in inputs)
        {
            text.Append("| `");
            text.Append(input.Id);
            text.Append("` | ");
            text.Append(input.Description);
            text.Append(" | ");
            text.Append(input.Width.ToString(CultureInfo.InvariantCulture));
            text.Append('x');
            text.Append(input.Height.ToString(CultureInfo.InvariantCulture));
            text.Append(" |\r\n");
        }

        text.Append("\r\nИзображения — файлы `testdata/<имя>.png`, чёрный пиксель активен. ");
        text.Append("Листы шрифтов — `testdata/font_sheet_6x8.png`, `font_sheet_8x8.png`, `font_sheet_12x16.png`: ");
        text.Append("16 символов в строке, 16 строк, без отступов и интервалов, первый код 0x00. ");
        text.Append("Символ c занимает байты c*N ... c*N+N-1. Пустой символ — байты фона: 0x00, с инверсией 0xFF.\r\n\r\n");
        text.Append("Глиф листа — растр 5x7. В ячейке 6x8 он стоит в левом верхнем углу, ");
        text.Append("в 8x8 — со столбцом фона слева, в 12x16 — каждый пиксель глифа растянут в блок 2x2 ");
        text.Append("с отступом 1 пиксель слева и сверху. Латиница 0x20–0x7E, Ё (0xA8), ё (0xB8) и кириллица ");
        text.Append("0xC0–0xFF нарисованы. Строчная кириллица совпадает с заглавной. Остальные коды, кроме 0x00, ");
        text.Append("помечены битами кода. Код 0x00 пустой.\r\n\r\n");
        text.Append("Пресет берёт один размер и одну комбинацию, но эталон содержит все комбинации этого размера:\r\n\r\n");
        text.Append("| Пресет | Вход | Комбинация пресета |\r\n");
        text.Append("|---|---|---|\r\n");
        text.Append("| WG240128A | `test_pattern_240x128` | `h_msb_8` |\r\n");
        text.Append("| W0240128 | `test_pattern_240x128` | `v_lsb_pages` |\r\n");
        text.Append("| RG12864F | `test_pattern_128x64` | `v_lsb_pages` |\r\n");
        text.Append("| RET012864DGPP3N | `test_pattern_128x64` | `v_lsb_pages` |\r\n");
        text.Append("| OLED128X64-0.96 | `test_pattern_128x64` | `v_lsb_pages` |\r\n");
        text.Append("| HT1.3-OLED-BW / HR0161 | `test_pattern_128x64` | `v_lsb_pages` |\r\n\r\n");
        text.Append("## Комбинации\r\n\r\n");
        text.Append("| Файл | Упаковка |\r\n");
        text.Append("|---|---|\r\n");
        foreach (RefOptions options in RefOptions.AllCombinations)
        {
            text.Append("| `");
            text.Append(options.Name);
            text.Append(".bin`, `");
            text.Append(options.Name);
            text.Append(".c` | ");
            text.Append(Describe(options));
            text.Append(" |\r\n");
        }

        text.Append("\r\n## Размер массива, байт\r\n\r\n");
        text.Append("Число байт одно для пары «вход + комбинация» и записано в комментарии `.c`. ");
        text.Append("Горизонтальный режим: высота, умноженная на округление вверх (ширина / бит в байте). ");
        text.Append("Вертикальный: ширина, умноженная на округление вверх (высота / 8). Для шрифта результат умножается на 256.\r\n");
        return text.ToString();
    }

    private sealed record WrittenInput(string Id, string Description, int Width, int Height, IReadOnlyList<string> Files);
}
