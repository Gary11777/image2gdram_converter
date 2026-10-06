using System.Text;
using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Projects;
using Image2Gdram.Core.Settings;

namespace Image2Gdram.Core.Tests.Projects;

public sealed class ProjectSerializerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "i2g_project_" + Guid.NewGuid().ToString("N"));

    public ProjectSerializerTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Save_then_load_restores_both_tabs_and_the_second_save_matches_byte_for_byte()
    {
        string image = Path.Combine(_root, "logo.png");
        File.WriteAllBytes(image, new byte[] { 9, 8, 7 });
        byte[] before = File.ReadAllBytes(image);
        ProjectDocument project = Sample(image, Path.Combine(_root, "missing-sheet.png"), Path.Combine(_root, "missing-font.c"));
        string path = Path.Combine(_root, "work.iiu");

        ProjectSerializer.Save(project, path);
        byte[] first = File.ReadAllBytes(path);
        ProjectLoadResult loaded = ProjectSerializer.Load(path);

        Assert.Equal(before, File.ReadAllBytes(image));
        Assert.Equal(2, loaded.Diagnostics.Count);
        Assert.Equal(ProjectSources.Sheet, loaded.Diagnostics[0].Arguments[0]);
        Assert.Equal(ProjectSources.Import, loaded.Diagnostics[1].Arguments[0]);
        Assert.All(loaded.Diagnostics, diagnostic => Assert.Equal(DiagnosticCode.ProjectSourceNotFound, diagnostic.Code));
        AssertSame(project, loaded.Project);
        Assert.Null(loaded.Project.Image.Parameters.Output.GeneratedAt);

        ProjectSerializer.Save(loaded.Project, path);
        Assert.Equal(first, File.ReadAllBytes(path));

        Assert.False(first.AsSpan(0, 3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }));
        string text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(first);
        Assert.Contains("\"formatVersion\": 1", text, StringComparison.Ordinal);
        Assert.Contains("\u041f\u0440\u0438\u0432\u0435\u0442", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u041F", text, StringComparison.Ordinal);
        Assert.Contains("\"direction\": \"Horizontal\"", text, StringComparison.Ordinal);
        Assert.Contains("\"pixelFormat\": \"Rgb565\"", text, StringComparison.Ordinal);
        Assert.Contains("\"bitOrder\": \"MsbFirst\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("generatedAt", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"glyphs\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_image_still_loads_parameters_and_edits()
    {
        string missing = Path.Combine(_root, "gone.png");
        var edits = new FramePixelOverrides();
        edits.ForFrame(0).Set(4, 5, true);
        var project = new ProjectDocument(
            new ImageProjectTab(ImageTabParameters.CreateDefault() with { SelectedFrame = 4 }, missing, edits),
            FontProjectTab.CreateDefault());

        string path = Path.Combine(_root, "gone.iiu");
        ProjectSerializer.Save(project, path);
        ProjectLoadResult loaded = ProjectSerializer.Load(path);

        Assert.Equal(DiagnosticCode.ProjectSourceNotFound, Assert.Single(loaded.Diagnostics).Code);
        Assert.Equal(ProjectSources.Image, loaded.Diagnostics[0].Arguments[0]);
        Assert.Equal(Path.GetFullPath(missing), loaded.Project.Image.SourcePath);
        Assert.Equal(4, loaded.Project.Image.Parameters.SelectedFrame);
        Assert.Equal(new[] { (4, 5, true) }, loaded.Project.Image.Edits.ForFrame(0).Entries().ToArray());
        Assert.Equal(FontCellSize.Cell6x8, loaded.Project.Font.Table.Cell);
        Assert.Equal(GlyphOrigin.None, loaded.Project.Font.Table.GetOrigin(0x41));
    }

    [Fact]
    public void Relative_path_wins_over_a_stale_absolute_path()
    {
        string dirA = Path.Combine(_root, "a");
        string dirB = Path.Combine(_root, "b");
        string dirC = Path.Combine(_root, "c");
        Directory.CreateDirectory(dirA);
        Directory.CreateDirectory(dirB);
        Directory.CreateDirectory(dirC);
        File.WriteAllBytes(Path.Combine(dirA, "pic.bin"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(dirB, "pic.bin"), new byte[] { 2 });
        var project = new ProjectDocument(
            new ImageProjectTab(ImageTabParameters.CreateDefault(), Path.Combine(dirA, "pic.bin")),
            FontProjectTab.CreateDefault());
        string projectA = Path.Combine(dirA, "work.iiu");
        ProjectSerializer.Save(project, projectA);
        File.Copy(projectA, Path.Combine(dirB, "work.iiu"));
        File.Copy(projectA, Path.Combine(dirC, "work.iiu"));

        ProjectLoadResult besideCopy = ProjectSerializer.Load(Path.Combine(dirB, "work.iiu"));
        Assert.Empty(besideCopy.Diagnostics);
        Assert.Equal(Path.GetFullPath(Path.Combine(dirB, "pic.bin")), besideCopy.Project.Image.SourcePath);

        ProjectLoadResult onlyAbsolute = ProjectSerializer.Load(Path.Combine(dirC, "work.iiu"));
        Assert.Empty(onlyAbsolute.Diagnostics);
        Assert.Equal(Path.GetFullPath(Path.Combine(dirA, "pic.bin")), onlyAbsolute.Project.Image.SourcePath);

        File.Delete(Path.Combine(dirA, "pic.bin"));
        ProjectLoadResult missing = ProjectSerializer.Load(Path.Combine(dirC, "work.iiu"));
        Assert.Equal(ProjectSources.Image, Assert.Single(missing.Diagnostics).Arguments[0]);
        Assert.Equal(Path.GetFullPath(Path.Combine(dirA, "pic.bin")), missing.Project.Image.SourcePath);
        Assert.Equal(ImageTabParameters.CreateDefault().Processing, missing.Project.Image.Parameters.Processing);
    }

    [Fact]
    public void Corrupt_file_and_unknown_version_are_errors_and_the_file_stays()
    {
        string path = Path.Combine(_root, "bad.iiu");
        File.WriteAllText(path, "{");
        ProjectException broken = Assert.Throws<ProjectException>(() => ProjectSerializer.Load(path));
        Assert.Equal(ProjectError.Corrupt, broken.Error);
        Assert.Equal("{", File.ReadAllText(path));

        ProjectSerializer.Save(ProjectDocument.CreateDefault(), path);
        string original = File.ReadAllText(path);
        File.WriteAllText(path, original.Replace("\"formatVersion\": 1", "\"formatVersion\": 9", StringComparison.Ordinal));
        byte[] stored = File.ReadAllBytes(path);
        ProjectException version = Assert.Throws<ProjectException>(() => ProjectSerializer.Load(path));
        Assert.Equal(ProjectError.UnsupportedVersion, version.Error);
        Assert.Equal(stored, File.ReadAllBytes(path));

        ProjectException missing = Assert.Throws<ProjectException>(() => ProjectSerializer.Load(Path.Combine(_root, "no.iiu")));
        Assert.Equal(ProjectError.IoError, missing.Error);
    }

    [Fact]
    public void Font_table_cell_must_match_the_parameters()
    {
        var project = new ProjectDocument(
            ImageProjectTab.CreateDefault(),
            new FontProjectTab(
                FontTabParameters.CreateDefault() with { CellWidth = 12, CellHeight = 16 },
                new FontTable(FontCellSize.Cell6x8)));

        Assert.Throws<ArgumentException>(() => ProjectSerializer.Save(project, Path.Combine(_root, "mismatch.iiu")));
        Assert.False(File.Exists(Path.Combine(_root, "mismatch.iiu")));
    }

    private static ProjectDocument Sample(string imagePath, string sheetPath, string importPath)
    {
        var edits = new FramePixelOverrides();
        edits.ForFrame(2).Set(3, 4, true);
        edits.ForFrame(0).Set(1, 2, true);
        edits.ForFrame(0).Set(0, 0, false);
        var image = new ImageTabParameters
        {
            SelectedFrame = 3,
            ExportAllFrames = true,
            ColorScheme = ColorScheme.Lcd,
            Preset = new PresetBinding { BasedOn = "WG240128A" },
            Processing = ProcessingOptions.Default with
            {
                Background = BackgroundColor.Black,
                Rotation = Rotation.Rotate270,
                FlipHorizontal = true,
                FlipVertical = true,
                SizeMode = TargetSizeMode.Manual,
                TargetWidth = 13,
                TargetHeight = 11,
                Fit = FitMode.Fill,
                Alignment = Alignment.BottomRight,
                OffsetX = -4,
                OffsetY = 6,
                Resample = ResampleMode.NearestNeighbor,
                Binarize = BinarizeMode.Bayer8,
                Threshold = 200,
            },
            Packing = PackingOptions.Default with
            {
                PixelFormat = PixelFormat.Rgb565,
                Direction = PackDirection.Horizontal,
                BitOrder = BitOrder.MsbFirst,
                BitsPerByte = 6,
                PageTraversal = PageTraversal.ByColumns,
                Invert = true,
                ByteOrder = ByteOrder.LittleEndian,
            },
            Output = OutputOptions.Default with
            {
                Format = OutputFormat.Bin,
                ArrayName = "sprite_13x11",
                Encoding = OutputEncoding.Utf8NoBom,
                BytesPerLine = 7,
                AsmNumberFormat = AsmNumberFormat.Binary,
                AsmFileExtension = AsmFileExtension.Asm,
                Stm32ElementType = Stm32ElementType.UnsignedChar,
                IncludeDate = false,
                GeneratedAt = new DateTime(2020, 1, 2, 3, 4, 5),
            },
        };
        var font = new FontTabParameters
        {
            CellWidth = 8,
            CellHeight = 8,
            SourceKind = FontSourceKind.Import,
            Family = "Consolas",
            FontSizePx = 14,
            Bold = true,
            Italic = true,
            GlyphOffsetX = -3,
            GlyphOffsetY = 5,
            RenderMode = GlyphRenderMode.Aliased,
            GlyphThreshold = 40,
            Sheet = new SheetOptions
            {
                CellWidth = 10,
                CellHeight = 12,
                MarginX = 2,
                MarginY = 3,
                SpacingX = 1,
                SpacingY = 4,
                CharsPerRow = 8,
                FirstCode = 0x20,
                Threshold = 90,
            },
            ImportArrayName = "FONT_6X8",
            RangePresets = CharRangePreset.OtherCp1251,
            CustomCodes = new[] { 0x10, 0x98 },
            PreviewText = FontTabParameters.DefaultPreviewText,
            Packing = PackingOptions.Default with { Invert = true },
            Output = OutputOptions.Default with { ArrayName = "font_8x8", Format = OutputFormat.A51Include },
            ColorScheme = ColorScheme.Oled,
            Preset = PresetBinding.ForPreset(PresetCatalog.Shared.Find("OLED128X64-0.96")!),
        };
        return new ProjectDocument(
            new ImageProjectTab(image, imagePath, edits),
            new FontProjectTab(font, Table(), sheetPath, importPath));
    }

    private static FontTable Table()
    {
        var cell = FontCellSize.Cell8x8;
        var sources = new GlyphSet(cell);
        sources.Set(0x20, new MonoBitmap(8, 8));
        var marked = new MonoBitmap(8, 8);
        marked[0, 0] = true;
        marked[7, 3] = true;
        sources.Set(0xC0, marked);
        var table = new FontTable(cell);
        table.ReplaceSource(sources);
        table.SetManual(0x42, new MonoBitmap(8, 8));
        var drawn = new MonoBitmap(8, 8);
        drawn[1, 2] = true;
        table.SetManual(0x41, drawn);
        return table;
    }

    private static void AssertSame(ProjectDocument expected, ProjectDocument actual)
    {
        Assert.Equal(expected.Image.Parameters.Processing, actual.Image.Parameters.Processing);
        Assert.Equal(expected.Image.Parameters.Packing, actual.Image.Parameters.Packing);
        Assert.Equal(expected.Image.Parameters.Output with { GeneratedAt = null }, actual.Image.Parameters.Output);
        Assert.Equal(expected.Image.Parameters.ColorScheme, actual.Image.Parameters.ColorScheme);
        Assert.Equal(expected.Image.Parameters.Preset, actual.Image.Parameters.Preset);
        Assert.Equal(expected.Image.Parameters.SelectedFrame, actual.Image.Parameters.SelectedFrame);
        Assert.Equal(expected.Image.Parameters.ExportAllFrames, actual.Image.Parameters.ExportAllFrames);
        Assert.Equal(Path.GetFullPath(expected.Image.SourcePath!), actual.Image.SourcePath);
        AssertSameEdits(expected.Image.Edits, actual.Image.Edits);

        FontTabParameters expectedFont = expected.Font.Parameters;
        FontTabParameters actualFont = actual.Font.Parameters;
        Assert.Equal(expectedFont.CellWidth, actualFont.CellWidth);
        Assert.Equal(expectedFont.CellHeight, actualFont.CellHeight);
        Assert.Equal(expectedFont.SourceKind, actualFont.SourceKind);
        Assert.Equal(expectedFont.Family, actualFont.Family);
        Assert.Equal(expectedFont.FontSizePx, actualFont.FontSizePx);
        Assert.Equal(expectedFont.Bold, actualFont.Bold);
        Assert.Equal(expectedFont.Italic, actualFont.Italic);
        Assert.Equal(expectedFont.GlyphOffsetX, actualFont.GlyphOffsetX);
        Assert.Equal(expectedFont.GlyphOffsetY, actualFont.GlyphOffsetY);
        Assert.Equal(expectedFont.RenderMode, actualFont.RenderMode);
        Assert.Equal(expectedFont.GlyphThreshold, actualFont.GlyphThreshold);
        Assert.Equal(expectedFont.Sheet, actualFont.Sheet);
        Assert.Equal(expectedFont.ImportArrayName, actualFont.ImportArrayName);
        Assert.Equal(expectedFont.RangePresets, actualFont.RangePresets);
        Assert.Equal(expectedFont.CustomCodes.OrderBy(code => code).ToArray(), actualFont.CustomCodes);
        Assert.Equal(expectedFont.PreviewText, actualFont.PreviewText);
        Assert.Equal(expectedFont.Packing, actualFont.Packing);
        Assert.Equal(expectedFont.Output with { GeneratedAt = null }, actualFont.Output);
        Assert.Equal(expectedFont.ColorScheme, actualFont.ColorScheme);
        Assert.Equal(expectedFont.Preset, actualFont.Preset);
        Assert.Equal(Path.GetFullPath(expected.Font.SheetPath!), actual.Font.SheetPath);
        Assert.Equal(Path.GetFullPath(expected.Font.ImportPath!), actual.Font.ImportPath);
        AssertSameTable(expected.Font.Table, actual.Font.Table);
    }

    private static void AssertSameEdits(FramePixelOverrides expected, FramePixelOverrides actual)
    {
        Assert.Equal(expected.EditedFrames(), actual.EditedFrames());
        foreach (int frame in expected.EditedFrames())
        {
            Assert.Equal(expected.ForFrame(frame).Entries().ToArray(), actual.ForFrame(frame).Entries().ToArray());
        }
    }

    private static void AssertSameTable(FontTable expected, FontTable actual)
    {
        Assert.Equal(expected.Cell, actual.Cell);
        for (int code = 0; code < FontTable.CharCount; code++)
        {
            Assert.Equal(expected.GetOrigin(code), actual.GetOrigin(code));
            AssertGlyph(expected.GetSourceGlyph(code), actual.GetSourceGlyph(code));
            AssertGlyph(expected.GetManualGlyph(code), actual.GetManualGlyph(code));
        }
    }

    private static void AssertGlyph(MonoBitmap? expected, MonoBitmap? actual)
    {
        if (expected is null)
        {
            Assert.Null(actual);
            return;
        }

        Assert.NotNull(actual);
        Assert.True(expected.ContentEquals(actual));
    }
}
