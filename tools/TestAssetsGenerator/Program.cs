using Image2Gdram.TestAssets;

string output = args.Length > 0 ? args[0] : DefaultTestdataDirectory();
Directory.CreateDirectory(output);
Write("test_pattern_240x128.png", TestPattern.CreateScreen(240, 128));
Write("test_pattern_128x64.png", TestPattern.CreateScreen(128, 64));
Write("test_sprite_13x11.png", TestPattern.CreateSprite());
Console.WriteLine($"Тестовые изображения записаны в {output}");

void Write(string name, PatternBitmap image) => PngWriter.Write(Path.Combine(output, name), image);

static string DefaultTestdataDirectory()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "specification.md")))
        {
            return Path.Combine(directory.FullName, "testdata");
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Не найден корень репозитория (specification.md). Укажите папку аргументом.");
}
