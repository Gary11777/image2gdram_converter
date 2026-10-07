using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Projects;
using Image2Gdram.Core.Settings;

namespace Image2Gdram.Core.Persistence;

internal static class PresetMapping
{
    public static PresetDto FromPreset(Preset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        return new PresetDto
        {
            Name = preset.Name,
            Controller = preset.Controller,
            Width = preset.Width,
            Height = preset.Height,
            ColorScheme = preset.ColorScheme,
            Packing = ParameterMapping.FromPacking(preset.Packing),
        };
    }

    public static Preset ToPreset(PresetDto? dto, bool isBuiltIn)
    {
        if (dto is null || dto.Packing is null || string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new StoredDataException("A preset needs a name and packing.");
        }

        return new Preset(
            dto.Name,
            dto.Controller,
            dto.Width,
            dto.Height,
            ParameterMapping.ToPacking(dto.Packing),
            dto.ColorScheme,
            isBuiltIn);
    }
}

internal static class ParameterMapping
{
    private static readonly CharRangePreset[] RangeOrder =
    {
        CharRangePreset.Latin,
        CharRangePreset.Cyrillic,
        CharRangePreset.OtherCp1251,
    };

    public static ImageParametersDto FromImage(ImageTabParameters parameters)
    {
        ModelValidation.Ensure(parameters);
        return new ImageParametersDto
        {
            SelectedFrame = parameters.SelectedFrame,
            ExportAllFrames = parameters.ExportAllFrames,
            ColorScheme = parameters.ColorScheme,
            Preset = FromBinding(parameters.Preset),
            Processing = FromProcessing(parameters.Processing),
            Packing = FromPacking(parameters.Packing),
            Output = FromOutput(parameters.Output),
        };
    }

    public static ImageTabParameters ToImage(ImageParametersDto? dto, bool strict)
    {
        if (dto is null)
        {
            if (strict)
            {
                throw new StoredDataException("Image parameters are missing.");
            }

            return ImageTabParameters.CreateDefault();
        }

        var model = new ImageTabParameters
        {
            SelectedFrame = dto.SelectedFrame,
            ExportAllFrames = dto.ExportAllFrames,
            ColorScheme = dto.ColorScheme,
            Preset = ToBinding(dto.Preset),
            Processing = ToProcessing(dto.Processing, strict),
            Packing = ToPacking(dto.Packing, strict),
            Output = ToOutput(dto.Output, strict, "image"),
        };
        return Check(model);
    }

    public static FontParametersDto FromFont(FontTabParameters parameters)
    {
        ModelValidation.Ensure(parameters);
        return new FontParametersDto
        {
            CellWidth = parameters.CellWidth,
            CellHeight = parameters.CellHeight,
            SourceKind = parameters.SourceKind,
            Family = parameters.Family,
            FontSizePx = parameters.FontSizePx,
            Bold = parameters.Bold,
            Italic = parameters.Italic,
            GlyphOffsetX = parameters.GlyphOffsetX,
            GlyphOffsetY = parameters.GlyphOffsetY,
            RenderMode = parameters.RenderMode,
            GlyphThreshold = parameters.GlyphThreshold,
            Sheet = FromSheet(parameters.Sheet),
            ImportArrayName = parameters.ImportArrayName,
            RangePresets = WriteRanges(parameters.RangePresets),
            CustomCodes = parameters.CustomCodes.OrderBy(code => code).ToList(),
            PreviewText = parameters.PreviewText,
            PreviewScale = parameters.PreviewScale,
            ColorScheme = parameters.ColorScheme,
            Preset = FromBinding(parameters.Preset),
            Packing = FromPacking(parameters.Packing),
            Output = FromOutput(parameters.Output),
        };
    }

    public static FontTabParameters ToFont(FontParametersDto? dto, bool strict)
    {
        if (dto is null)
        {
            if (strict)
            {
                throw new StoredDataException("Font parameters are missing.");
            }

            return FontTabParameters.CreateDefault();
        }

        if (strict && (dto.Family is null || dto.PreviewText is null || dto.RangePresets is null))
        {
            throw new StoredDataException("Font parameters are incomplete.");
        }

        var model = new FontTabParameters
        {
            CellWidth = dto.CellWidth,
            CellHeight = dto.CellHeight,
            SourceKind = dto.SourceKind,
            Family = dto.Family ?? "",
            FontSizePx = dto.FontSizePx,
            Bold = dto.Bold,
            Italic = dto.Italic,
            GlyphOffsetX = dto.GlyphOffsetX,
            GlyphOffsetY = dto.GlyphOffsetY,
            RenderMode = dto.RenderMode,
            GlyphThreshold = dto.GlyphThreshold,
            Sheet = ToSheet(dto.Sheet, strict),
            ImportArrayName = dto.ImportArrayName,
            RangePresets = ReadRanges(dto.RangePresets),
            CustomCodes = ReadCodes(dto.CustomCodes),
            PreviewText = dto.PreviewText ?? FontTabParameters.DefaultPreviewText,
            PreviewScale = dto.PreviewScale == 0 ? 2 : dto.PreviewScale,
            ColorScheme = dto.ColorScheme,
            Preset = ToBinding(dto.Preset),
            Packing = ToPacking(dto.Packing, strict),
            Output = ToOutput(dto.Output, strict, "font"),
        };
        return Check(model);
    }

    public static PackingDto FromPacking(PackingOptions packing) => new()
    {
        PixelFormat = packing.PixelFormat,
        Direction = packing.Direction,
        BitOrder = packing.BitOrder,
        BitsPerByte = packing.BitsPerByte,
        PageTraversal = packing.PageTraversal,
        Invert = packing.Invert,
        ByteOrder = packing.ByteOrder,
    };

    public static PackingOptions ToPacking(PackingDto dto) => ToPacking(dto, strict: true);

    private static PackingOptions ToPacking(PackingDto? dto, bool strict)
    {
        if (dto is null)
        {
            if (strict)
            {
                throw new StoredDataException("Packing parameters are missing.");
            }

            return PackingOptions.Default;
        }

        return new PackingOptions
        {
            PixelFormat = dto.PixelFormat,
            Direction = dto.Direction,
            BitOrder = dto.BitOrder,
            BitsPerByte = dto.BitsPerByte,
            PageTraversal = dto.PageTraversal,
            Invert = dto.Invert,
            ByteOrder = dto.ByteOrder,
        };
    }

    private static ProcessingDto FromProcessing(ProcessingOptions options) => new()
    {
        Background = options.Background,
        Rotation = options.Rotation,
        FlipHorizontal = options.FlipHorizontal,
        FlipVertical = options.FlipVertical,
        SizeMode = options.SizeMode,
        TargetWidth = options.TargetWidth,
        TargetHeight = options.TargetHeight,
        Fit = options.Fit,
        Alignment = options.Alignment,
        OffsetX = options.OffsetX,
        OffsetY = options.OffsetY,
        Resample = options.Resample,
        Binarize = options.Binarize,
        Threshold = options.Threshold,
    };

    private static ProcessingOptions ToProcessing(ProcessingDto? dto, bool strict)
    {
        if (dto is null)
        {
            if (strict)
            {
                throw new StoredDataException("Processing parameters are missing.");
            }

            return ProcessingOptions.Default;
        }

        return new ProcessingOptions
        {
            Background = dto.Background,
            Rotation = dto.Rotation,
            FlipHorizontal = dto.FlipHorizontal,
            FlipVertical = dto.FlipVertical,
            SizeMode = dto.SizeMode,
            TargetWidth = dto.TargetWidth,
            TargetHeight = dto.TargetHeight,
            Fit = dto.Fit,
            Alignment = dto.Alignment,
            OffsetX = dto.OffsetX,
            OffsetY = dto.OffsetY,
            Resample = dto.Resample,
            Binarize = dto.Binarize,
            Threshold = dto.Threshold,
        };
    }

    private static OutputDto FromOutput(OutputOptions options) => new()
    {
        Format = options.Format,
        ArrayName = options.ArrayName,
        Encoding = options.Encoding,
        BytesPerLine = options.BytesPerLine,
        AsmNumberFormat = options.AsmNumberFormat,
        AsmFileExtension = options.AsmFileExtension,
        Stm32ElementType = options.Stm32ElementType,
        IncludeDate = options.IncludeDate,
    };

    private static OutputOptions ToOutput(OutputDto? dto, bool strict, string defaultArrayName)
    {
        if (dto is null)
        {
            if (strict)
            {
                throw new StoredDataException("Output parameters are missing.");
            }

            return OutputOptions.Default with { ArrayName = defaultArrayName };
        }

        return new OutputOptions
        {
            Format = dto.Format,
            ArrayName = dto.ArrayName ?? defaultArrayName,
            Encoding = dto.Encoding,
            BytesPerLine = dto.BytesPerLine,
            AsmNumberFormat = dto.AsmNumberFormat,
            AsmFileExtension = dto.AsmFileExtension,
            Stm32ElementType = dto.Stm32ElementType,
            IncludeDate = dto.IncludeDate,
        };
    }

    private static SheetDto FromSheet(SheetOptions sheet) => new()
    {
        CellWidth = sheet.CellWidth,
        CellHeight = sheet.CellHeight,
        MarginX = sheet.MarginX,
        MarginY = sheet.MarginY,
        SpacingX = sheet.SpacingX,
        SpacingY = sheet.SpacingY,
        CharsPerRow = sheet.CharsPerRow,
        FirstCode = sheet.FirstCode,
        Threshold = sheet.Threshold,
    };

    private static SheetOptions ToSheet(SheetDto? dto, bool strict)
    {
        if (dto is null)
        {
            if (strict)
            {
                throw new StoredDataException("Sheet parameters are missing.");
            }

            return new SheetOptions();
        }

        return new SheetOptions
        {
            CellWidth = dto.CellWidth,
            CellHeight = dto.CellHeight,
            MarginX = dto.MarginX,
            MarginY = dto.MarginY,
            SpacingX = dto.SpacingX,
            SpacingY = dto.SpacingY,
            CharsPerRow = dto.CharsPerRow,
            FirstCode = dto.FirstCode,
            Threshold = dto.Threshold,
        };
    }

    private static PresetBindingDto FromBinding(PresetBinding binding)
    {
        string? name = string.IsNullOrWhiteSpace(binding.Name) ? null : binding.Name.Trim();
        string? basedOn = name is null && !string.IsNullOrWhiteSpace(binding.BasedOn) ? binding.BasedOn.Trim() : null;
        return new PresetBindingDto { Name = name, BasedOn = basedOn };
    }

    private static PresetBinding ToBinding(PresetBindingDto? dto)
    {
        if (dto is null)
        {
            return PresetBinding.Custom;
        }

        string? name = string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name.Trim();
        string? basedOn = name is null && !string.IsNullOrWhiteSpace(dto.BasedOn) ? dto.BasedOn.Trim() : null;
        return new PresetBinding { Name = name, BasedOn = basedOn };
    }

    private static List<string> WriteRanges(CharRangePreset presets)
    {
        var names = new List<string>();
        foreach (CharRangePreset flag in RangeOrder)
        {
            if (presets.HasFlag(flag))
            {
                names.Add(flag.ToString());
            }
        }

        return names;
    }

    private static CharRangePreset ReadRanges(List<string>? names)
    {
        if (names is null)
        {
            return CharRangePreset.Latin | CharRangePreset.Cyrillic;
        }

        CharRangePreset result = CharRangePreset.None;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in names)
        {
            if (!seen.Add(name)
                || !Enum.TryParse(name, ignoreCase: false, out CharRangePreset flag)
                || flag.ToString() != name
                || flag is not (CharRangePreset.Latin or CharRangePreset.Cyrillic or CharRangePreset.OtherCp1251))
            {
                throw new StoredDataException($"Unknown character range '{name}'.");
            }

            result |= flag;
        }

        return result;
    }

    private static int[] ReadCodes(List<int>? codes) =>
        codes is null ? Array.Empty<int>() : codes.OrderBy(code => code).ToArray();

    private static T Check<T>(T model)
    {
        try
        {
            switch (model)
            {
                case ImageTabParameters image:
                    ModelValidation.Ensure(image);
                    break;
                case FontTabParameters font:
                    ModelValidation.Ensure(font);
                    break;
            }
        }
        catch (ArgumentException ex)
        {
            throw new StoredDataException(ex.Message, ex);
        }

        return model;
    }
}

internal static class SettingsMapping
{
    public static byte[] Write(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ModelValidation.Ensure(settings.Image.Parameters);
        ModelValidation.Ensure(settings.Font.Parameters);
        var dto = new SettingsFileDto
        {
            FormatVersion = AppSettings.FormatVersion,
            Language = NormalizeLanguage(settings.Language),
            Image = new ImageSettingsDto
            {
                SourcePath = Clean(settings.Image.SourcePath),
                Parameters = ParameterMapping.FromImage(settings.Image.Parameters),
            },
            Font = new FontSettingsDto
            {
                SheetPath = Clean(settings.Font.SheetPath),
                ImportPath = Clean(settings.Font.ImportPath),
                Parameters = ParameterMapping.FromFont(settings.Font.Parameters),
            },
            UserPresets = settings.UserPresets.Presets.Select(PresetMapping.FromPreset).ToList(),
            RecentFiles = NormalizeRecent(settings.RecentFiles),
            Folders = FromFolders(settings.Folders),
        };
        return JsonFormat.Write(dto);
    }

    public static AppSettings Read(byte[] content)
    {
        SettingsFileDto dto = JsonFormat.Parse<SettingsFileDto>(content);
        if (dto.FormatVersion != AppSettings.FormatVersion)
        {
            throw new StoredDataException($"Settings version {dto.FormatVersion} is not supported.");
        }

        var store = new UserPresetStore(PresetCatalog.Shared);
        if (dto.UserPresets is not null)
        {
            foreach (PresetDto preset in dto.UserPresets)
            {
                store.Add(PresetMapping.ToPreset(preset, isBuiltIn: false));
            }
        }

        var settings = new AppSettings(store)
        {
            Language = NormalizeLanguage(dto.Language),
            Image = dto.Image is null
                ? ImageSettingsTab.CreateDefault()
                : new ImageSettingsTab
                {
                    SourcePath = Clean(dto.Image.SourcePath),
                    Parameters = ParameterMapping.ToImage(dto.Image.Parameters, strict: false),
                },
            Font = dto.Font is null
                ? FontSettingsTab.CreateDefault()
                : new FontSettingsTab
                {
                    SheetPath = Clean(dto.Font.SheetPath),
                    ImportPath = Clean(dto.Font.ImportPath),
                    Parameters = ParameterMapping.ToFont(dto.Font.Parameters, strict: false),
                },
            Folders = ToFolders(dto.Folders),
        };
        settings.RecentFiles.AddRange(NormalizeRecent(dto.RecentFiles));
        return settings;
    }

    public static string NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? AppSettings.DefaultLanguage : language.Trim();

    public static List<string> NormalizeRecent(IEnumerable<string>? files)
    {
        var result = new List<string>();
        if (files is null)
        {
            return result;
        }

        foreach (string file in files)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                continue;
            }

            string trimmed = file.Trim();
            if (result.Exists(item => string.Equals(item, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            result.Add(trimmed);
            if (result.Count == AppSettings.MaxRecentFiles)
            {
                break;
            }
        }

        return result;
    }

    private static FoldersDto FromFolders(LastFolders folders) => new()
    {
        OpenImage = Clean(folders.OpenImage),
        OpenFont = Clean(folders.OpenFont),
        SaveOutput = Clean(folders.SaveOutput),
        OpenProject = Clean(folders.OpenProject),
        SaveProject = Clean(folders.SaveProject),
    };

    private static LastFolders ToFolders(FoldersDto? dto) => dto is null
        ? new LastFolders()
        : new LastFolders
        {
            OpenImage = Clean(dto.OpenImage),
            OpenFont = Clean(dto.OpenFont),
            SaveOutput = Clean(dto.SaveOutput),
            OpenProject = Clean(dto.OpenProject),
            SaveProject = Clean(dto.SaveProject),
        };

    private static string? Clean(string? path) => string.IsNullOrWhiteSpace(path) ? null : path.Trim();
}

internal static class ProjectMapping
{
    public static ProjectFileDto ToDto(ProjectDocument project, string projectFile)
    {
        ModelValidation.Ensure(project.Image.Parameters);
        ModelValidation.Ensure(project.Font.Parameters);
        if (!FontCellSize.TryGet(project.Font.Parameters.CellWidth, project.Font.Parameters.CellHeight, out FontCellSize cell)
            || cell != project.Font.Table.Cell)
        {
            throw new ArgumentException("Font table cell does not match font parameters.", nameof(project));
        }

        return new ProjectFileDto
        {
            FormatVersion = ProjectDocument.FormatVersion,
            Image = new ProjectImageDto
            {
                Parameters = ParameterMapping.FromImage(project.Image.Parameters),
                Source = PathMapping.Capture(projectFile, project.Image.SourcePath),
                Edits = EditMapping.Capture(project.Image.Edits),
            },
            Font = new ProjectFontDto
            {
                Parameters = ParameterMapping.FromFont(project.Font.Parameters),
                Sheet = PathMapping.Capture(projectFile, project.Font.SheetPath),
                Import = PathMapping.Capture(projectFile, project.Font.ImportPath),
                Glyphs = GlyphMapping.Capture(project.Font.Table),
            },
        };
    }

    public static ProjectLoadResult ToDocument(string projectFile, ProjectFileDto dto)
    {
        if (dto.Image is null || dto.Font is null)
        {
            throw new StoredDataException("A project must contain both tabs.");
        }

        string projectDir = Path.GetDirectoryName(projectFile)
            ?? throw new StoredDataException("Project path has no directory.");
        var diagnostics = new List<Diagnostic>();
        string? imagePath = PathMapping.Resolve(projectDir, dto.Image.Source, out bool imageMissing);
        string? sheetPath = PathMapping.Resolve(projectDir, dto.Font.Sheet, out bool sheetMissing);
        string? importPath = PathMapping.Resolve(projectDir, dto.Font.Import, out bool importMissing);
        AddMissing(diagnostics, ProjectSources.Image, imagePath, imageMissing);
        AddMissing(diagnostics, ProjectSources.Sheet, sheetPath, sheetMissing);
        AddMissing(diagnostics, ProjectSources.Import, importPath, importMissing);

        FontTabParameters fontParameters = ParameterMapping.ToFont(dto.Font.Parameters, strict: true);
        var document = new ProjectDocument(
            new ImageProjectTab(
                ParameterMapping.ToImage(dto.Image.Parameters, strict: true),
                imagePath,
                EditMapping.ToModel(dto.Image.Edits)),
            new FontProjectTab(
                fontParameters,
                GlyphMapping.ToTable(fontParameters.Cell, dto.Font.Glyphs),
                sheetPath,
                importPath));
        return new ProjectLoadResult(document, diagnostics);
    }

    private static void AddMissing(List<Diagnostic> diagnostics, string role, string? path, bool missing)
    {
        if (!missing)
        {
            return;
        }

        diagnostics.Add(new Diagnostic(
            DiagnosticCode.ProjectSourceNotFound,
            DiagnosticSeverity.Warning,
            new[] { role, path ?? string.Empty }));
    }
}

internal static class PathMapping
{
    public static StoredPathDto? Capture(string projectFile, string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return null;
        }

        string absolute = Path.GetFullPath(sourcePath);
        string projectDir = Path.GetDirectoryName(Path.GetFullPath(projectFile))
            ?? throw new ArgumentException("Project path has no directory.", nameof(projectFile));
        string relative = Path.GetRelativePath(projectDir, absolute).Replace('\\', '/');
        return new StoredPathDto { Absolute = absolute, Relative = relative };
    }

    public static string? Resolve(string projectDirectory, StoredPathDto? stored, out bool missing)
    {
        missing = false;
        string? relative = Clean(stored?.Relative);
        string? absolute = Clean(stored?.Absolute);
        if (relative is null && absolute is null)
        {
            return null;
        }

        if (relative is not null)
        {
            string full = Full(projectDirectory, relative);
            if (File.Exists(full))
            {
                return full;
            }
        }

        if (absolute is not null)
        {
            string full = Path.GetFullPath(absolute);
            if (File.Exists(full))
            {
                return full;
            }
        }

        missing = true;
        return absolute is not null ? Path.GetFullPath(absolute) : Full(projectDirectory, relative!);
    }

    private static string Full(string projectDirectory, string relative) =>
        Path.GetFullPath(Path.Combine(projectDirectory, relative.Replace('/', Path.DirectorySeparatorChar)));

    private static string? Clean(string? path) => string.IsNullOrWhiteSpace(path) ? null : path.Trim();
}

internal static class EditMapping
{
    public static List<FrameEditsDto> Capture(FramePixelOverrides edits)
    {
        var frames = new List<FrameEditsDto>();
        foreach (int frame in edits.EditedFrames())
        {
            var pixels = new List<PixelDto>();
            foreach ((int x, int y, bool active) in edits.ForFrame(frame).Entries())
            {
                pixels.Add(new PixelDto { X = x, Y = y, Active = active });
            }

            frames.Add(new FrameEditsDto { Frame = frame, Pixels = pixels });
        }

        return frames;
    }

    public static FramePixelOverrides ToModel(List<FrameEditsDto>? frames)
    {
        var edits = new FramePixelOverrides();
        if (frames is null)
        {
            return edits;
        }

        var seen = new HashSet<int>();
        foreach (FrameEditsDto frame in frames.OrderBy(item => item.Frame))
        {
            if (frame.Frame < 0 || !seen.Add(frame.Frame) || frame.Pixels is null)
            {
                throw new StoredDataException("Pixel edits contain a repeated, negative or missing frame.");
            }

            if (frame.Pixels.Count == 0)
            {
                continue;
            }

            PixelOverrides target = edits.ForFrame(frame.Frame);
            foreach (PixelDto pixel in frame.Pixels)
            {
                target.Set(pixel.X, pixel.Y, pixel.Active);
            }
        }

        return edits;
    }
}

internal static class GlyphMapping
{
    public static List<GlyphDto> Capture(FontTable table)
    {
        var glyphs = new List<GlyphDto>(FontTable.CharCount);
        for (int code = 0; code < FontTable.CharCount; code++)
        {
            glyphs.Add(new GlyphDto
            {
                Source = Format(table.GetSourceGlyph(code)),
                Manual = Format(table.GetManualGlyph(code)),
            });
        }

        return glyphs;
    }

    public static FontTable ToTable(FontCellSize cell, List<GlyphDto>? glyphs)
    {
        if (glyphs is null || glyphs.Count != FontTable.CharCount)
        {
            throw new StoredDataException($"A font project must contain {FontTable.CharCount} glyphs.");
        }

        var sources = new GlyphSet(cell);
        var manuals = new List<(int Code, MonoBitmap Bitmap)>();
        for (int code = 0; code < FontTable.CharCount; code++)
        {
            GlyphDto glyph = glyphs[code] ?? throw new StoredDataException($"Glyph {code} is missing.");
            if (glyph.Source is not null)
            {
                sources.Set(code, Parse(glyph.Source, cell));
            }

            if (glyph.Manual is not null)
            {
                manuals.Add((code, Parse(glyph.Manual, cell)));
            }
        }

        var table = new FontTable(cell);
        table.ReplaceSource(sources);
        foreach ((int code, MonoBitmap bitmap) in manuals)
        {
            table.SetManual(code, bitmap);
        }

        return table;
    }

    private static string? Format(MonoBitmap? bitmap)
    {
        if (bitmap is null)
        {
            return null;
        }

        var chars = new char[bitmap.Width * bitmap.Height];
        int index = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            ReadOnlySpan<bool> row = bitmap.GetRow(y);
            for (int x = 0; x < row.Length; x++)
            {
                chars[index++] = row[x] ? '1' : '0';
            }
        }

        return new string(chars);
    }

    private static MonoBitmap Parse(string text, FontCellSize cell)
    {
        int expected = cell.Width * cell.Height;
        if (text.Length != expected)
        {
            throw new StoredDataException($"Glyph must be {expected} bits, got {text.Length}.");
        }

        var bitmap = new MonoBitmap(cell.Width, cell.Height);
        int index = 0;
        for (int y = 0; y < cell.Height; y++)
        {
            for (int x = 0; x < cell.Width; x++)
            {
                char bit = text[index++];
                if (bit == '1')
                {
                    bitmap[x, y] = true;
                }
                else if (bit != '0')
                {
                    throw new StoredDataException("Glyph bits must be 0 or 1.");
                }
            }
        }

        return bitmap;
    }
}
