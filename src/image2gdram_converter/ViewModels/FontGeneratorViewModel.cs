using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Editing;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Fonts.Import;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Projects;
using Image2Gdram.Core.Settings;
using Image2Gdram.Core.Text;
using image2gdram_converter.Services;

namespace image2gdram_converter.ViewModels;

/// <summary>
/// Вкладка «Генератор шрифтов»: источники, диапазоны, таблица 256 символов, редактор и код.
/// </summary>
public sealed partial class FontGeneratorViewModel : ObservableValidator, ICodeSurface
{
    private enum SourceJob
    {
        None,
        Render,
        Clear,
    }

    private enum RenderIntent
    {
        KeepPending,
        RenderIfPossible,
        Drop,
    }

    private readonly ILocalizationService _loc;
    private readonly IDialogService _dialogs;
    private readonly IFileDialogService _files;
    private readonly IClipboardService _clipboard;
    private readonly IRecalcScheduler _scheduler;
    private readonly IImageDecoder _decoder;
    private readonly GlyphOutlineCache _cache;
    private readonly PresetCatalog _catalog;
    private readonly UserPresetStore _users;
    private readonly PackerRegistry _packers = PackerRegistry.CreateDefault();
    private readonly OutputGeneratorRegistry _generators = OutputGeneratorRegistry.CreateDefault();
    private readonly EditHistory _history = new();

    private FontTable _table = new(FontCellSize.Cell6x8);
    private RgbaImage? _sheet;
    private string? _sheetPath;
    private string? _importPath;
    private string? _importArrayName;
    private byte[]? _importBytes;
    private string? _fontFolder;
    private string? _outputFolder;
    private bool _suppress;
    private bool _nameCustomized;
    private bool _keepHighlight;
    private SourceJob _job = SourceJob.None;
    private int _jobId;
    private IReadOnlyList<int> _missing = Array.Empty<int>();
    private IReadOnlyList<Diagnostic> _sourceDiagnostics = Array.Empty<Diagnostic>();
    private byte[]? _fontBytes;
    private int _codeIndex;
    private string _codeText = string.Empty;
    private string _hoverText = string.Empty;
    private string _warningText = string.Empty;
    private int _highlightStart;
    private int _highlightLength;
    private int _tableRevision;

    private FontCellSize _cell = FontCellSize.Cell6x8;
    private FontSourceKind _sourceKind = FontSourceKind.TrueType;
    private string _family = string.Empty;
    private int _fontSize = 8;
    private string _fontSizeText = "8";
    private bool _bold;
    private bool _italic;
    private int _offsetX;
    private int _offsetY;
    private string _offsetXText = "0";
    private string _offsetYText = "0";
    private GlyphRenderMode _renderMode = GlyphRenderMode.Antialiased;
    private int _glyphThreshold = 128;
    private string _glyphThresholdText = "128";
    private int _sheetWidth = 6;
    private int _sheetHeight = 8;
    private string _sheetWidthText = "6";
    private string _sheetHeightText = "8";
    private int _marginX;
    private int _marginY;
    private int _spacingX;
    private int _spacingY;
    private string _marginXText = "0";
    private string _marginYText = "0";
    private string _spacingXText = "0";
    private string _spacingYText = "0";
    private int _charsPerRow = 16;
    private string _charsPerRowText = "16";
    private int _firstCode;
    private string _firstCodeText = "0";
    private int _sheetThreshold = 128;
    private string _sheetThresholdText = "128";
    private CharRangePreset _ranges = CharRangePreset.Latin | CharRangePreset.Cyrillic;
    private int[] _customCodes = Array.Empty<int>();
    private string _customRangeText = string.Empty;
    private string _previewText = FontTabParameters.DefaultPreviewText;
    private int _previewScale = 2;
    private PackDirection _direction = PackDirection.Vertical;
    private BitOrder _bitOrder = BitOrder.LsbFirst;
    private int _bits = 8;
    private PageTraversal _traversal = PageTraversal.ByPages;
    private bool _invert;
    private ColorScheme _scheme = ColorScheme.Oled;
    private PresetBinding _binding = PresetBinding.Custom;
    private OutputFormat _format = OutputFormat.CKeilC51;
    private string _arrayName = "font";
    private OutputEncoding _encoding = OutputEncoding.Cp1251;
    private int _bytesPerLine = 16;
    private string _bytesText = "16";
    private AsmNumberFormat _asmNumbers = AsmNumberFormat.Hex;
    private AsmFileExtension _asmExtension = AsmFileExtension.A51;
    private Stm32ElementType _stm32 = Stm32ElementType.Uint8T;
    private bool _includeDate = true;
    private bool _showGrid = true;
    private bool _fitWindow = true;
    private int _scale = 8;
    private ZoomChoice? _zoom;

    public FontGeneratorViewModel(
        ILocalizationService localization,
        IDialogService dialogs,
        IFileDialogService files,
        IClipboardService clipboard,
        IRecalcScheduler scheduler,
        IImageDecoder decoder,
        IGlyphOutlineProvider outlines,
        PresetCatalog catalog,
        UserPresetStore userPresets,
        FontSettingsTab settings)
    {
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(clipboard);
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(decoder);
        ArgumentNullException.ThrowIfNull(outlines);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(userPresets);
        ArgumentNullException.ThrowIfNull(settings);
        _loc = localization;
        _dialogs = dialogs;
        _files = files;
        _clipboard = clipboard;
        _scheduler = scheduler;
        _decoder = decoder;
        _cache = new GlyphOutlineCache(outlines);
        _catalog = catalog;
        _users = userPresets;
        SourceKinds = OptionLists.FontSources(localization);
        RenderModes = OptionLists.RenderModes(localization);
        Cells = OptionLists.Cells(localization);
        Directions = OptionLists.Directions(localization);
        BitOrders = OptionLists.BitOrders(localization);
        BitCounts = OptionLists.BitCounts(localization);
        Traversals = OptionLists.Traversals(localization);
        Schemes = OptionLists.Schemes(localization);
        Formats = OptionLists.Formats(localization);
        Encodings = OptionLists.Encodings(localization);
        AsmNumberFormats = OptionLists.AsmNumbers(localization);
        AsmExtensions = OptionLists.AsmExtensions(localization);
        Stm32Types = OptionLists.Stm32Types(localization);
        PreviewScales = OptionLists.PreviewScales(localization);
        Zooms = OptionLists.Zooms(localization);
        _zoom = Zooms[0];
        Families = new ObservableCollection<string>(outlines.GetInstalledFamilies());
        Presets = new PresetSelectorViewModel(localization, catalog, userPresets, ChoosePreset);
        Presets.SelectionChanged += () =>
        {
            RenamePresetCommand.NotifyCanExecuteChanged();
            DeletePresetCommand.NotifyCanExecuteChanged();
        };
        _sheetPath = settings.SheetPath;
        _importPath = settings.ImportPath;
        ApplyParameters(settings.Parameters, RenderIntent.RenderIfPossible);
        _table = new FontTable(_cell);
        Editor = new GlyphEditorViewModel(
            clipboard,
            () => _invert,
            code => _table.GetGlyph(code),
            SetManual,
            FormatCode);
        EnsureFamilyListed(_family);
        TouchTable();
        Schedule();
    }

    public GlyphEditorViewModel Editor { get; }

    public PresetSelectorViewModel Presets { get; }

    public ObservableCollection<string> Families { get; }

    public ObservableCollection<CodeFileItem> CodeFiles { get; } = new();

    public IReadOnlyList<Labeled<FontSourceKind>> SourceKinds { get; }

    public IReadOnlyList<Labeled<GlyphRenderMode>> RenderModes { get; }

    public IReadOnlyList<Labeled<FontCellSize>> Cells { get; }

    public IReadOnlyList<Labeled<PackDirection>> Directions { get; }

    public IReadOnlyList<Labeled<BitOrder>> BitOrders { get; }

    public IReadOnlyList<Labeled<int>> BitCounts { get; }

    public IReadOnlyList<Labeled<PageTraversal>> Traversals { get; }

    public IReadOnlyList<Labeled<ColorScheme>> Schemes { get; }

    public IReadOnlyList<Labeled<OutputFormat>> Formats { get; }

    public IReadOnlyList<Labeled<OutputEncoding>> Encodings { get; }

    public IReadOnlyList<Labeled<AsmNumberFormat>> AsmNumberFormats { get; }

    public IReadOnlyList<Labeled<AsmFileExtension>> AsmExtensions { get; }

    public IReadOnlyList<Labeled<Stm32ElementType>> Stm32Types { get; }

    public IReadOnlyList<Labeled<int>> PreviewScales { get; }

    public IReadOnlyList<ZoomChoice> Zooms { get; }

    public FontTable Table => _table;

    public int TableRevision => _tableRevision;

    public OutputDocument? Document { get; private set; }

    public Action? ParametersChanged { get; set; }

    public Action? EditsChanged { get; set; }

    public Action<string, SessionFolder>? FileUsed { get; set; }

    public Action? CommandsChanged { get; set; }

    public Action? PresetsChanged { get; set; }

    public Action? Activated { get; set; }

    public bool CanUndo => _history.CanUndo;

    public bool CanRedo => _history.CanRedo;

    public bool CanCopy => Document is not null && !HasErrors;

    public bool CanSaveOutput => CanCopy;

    public bool CanResetManual => _table.HasManualEdits;

    public bool HasUnsavedWork() =>
        _table.HasManualEdits || (_sourceKind == FontSourceKind.Import && HasSourceGlyph());

    public void SetFolders(string? openFont, string? saveOutput)
    {
        _fontFolder = openFont;
        _outputFolder = saveOutput;
    }

    public FontTabParameters ToParameters() => new()
    {
        CellWidth = _cell.Width,
        CellHeight = _cell.Height,
        SourceKind = _sourceKind,
        Family = _family,
        FontSizePx = _fontSize,
        Bold = _bold,
        Italic = _italic,
        GlyphOffsetX = _offsetX,
        GlyphOffsetY = _offsetY,
        RenderMode = _renderMode,
        GlyphThreshold = _glyphThreshold,
        Sheet = ToSheet(),
        ImportArrayName = _importArrayName,
        RangePresets = _ranges,
        CustomCodes = _customCodes,
        PreviewText = _previewText,
        PreviewScale = _previewScale,
        Packing = ToPacking(),
        Output = ToOutput(),
        ColorScheme = _scheme,
        Preset = _binding,
    };

    public FontSettingsTab ToSettings() => new()
    {
        Parameters = ToParameters(),
        SheetPath = _sheetPath,
        ImportPath = _importPath,
    };

    public FontProjectTab ToProject() => new(ToParameters(), _table.Clone(), _sheetPath, _importPath);

    public void Load(FontProjectTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        _sheet = null;
        _importBytes = null;
        _sheetPath = tab.SheetPath;
        _importPath = tab.ImportPath;
        ApplyParameters(tab.Parameters, RenderIntent.Drop);
        _table = tab.Table;
        if (_table.Cell != _cell)
        {
            _cell = _table.Cell;
        }

        _history.Clear();
        NotifyUndo();
        TouchTable();
        Schedule();
    }

    public void ResetSession()
    {
        _sheet = null;
        _sheetPath = null;
        _importPath = null;
        _importArrayName = null;
        _importBytes = null;
        _table = new FontTable(_cell);
        _history.Clear();
        _job = SourceJob.None;
        if (JobForCurrentKind() == SourceJob.Render)
        {
            _jobId++;
            _job = SourceJob.Render;
        }

        NotifyUndo();
        OnPropertyChanged(string.Empty);
        TouchTable();
        ParametersChanged?.Invoke();
        Schedule();
    }

    public void RefreshPresets()
    {
        if (!_binding.IsCustom && _binding.Name is string name && FindPreset(name) is null)
        {
            _binding = PresetBinding.Custom;
        }

        Presets.Show(_binding);
        OnPropertyChanged(nameof(PresetCaption));
    }

    public void LoadSheet(RgbaImage image, string path)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _sheet = image;
        _sheetPath = path;
        bool kindChanged = _sourceKind != FontSourceKind.Sheet;
        _sourceKind = FontSourceKind.Sheet;
        OnPropertyChanged(nameof(SourceKind));
        FileUsed?.Invoke(path, SessionFolder.OpenFont);
        Commit(preset: false, source: true, kindChanged);
    }

    public void ImportFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ApplyParsedImport(path, ReadArrays(path));
    }

    public void SetHover(int? x, int? y)
    {
        MonoBitmap? glyph = Editor.Glyph;
        if (x is null || y is null || glyph is null || _fontBytes is null
            || (uint)x.Value >= (uint)glyph.Width || (uint)y.Value >= (uint)glyph.Height)
        {
            HoverText = string.Empty;
            HighlightLength = 0;
            return;
        }

        PackingOptions packing = ToPacking();
        BitLocation location = _packers.Get(packing).Locate(x.Value, y.Value, glyph.Width, glyph.Height, packing);
        int per = _fontBytes.Length / FontTable.CharCount;
        int index = (Editor.SelectedCode * per) + location.ByteIndex;
        if ((uint)index >= (uint)_fontBytes.Length)
        {
            return;
        }

        HoverText = UserText.Hover(
            _loc,
            x.Value,
            y.Value,
            index,
            location.Bit,
            _fontBytes[index],
            glyph[x.Value, y.Value],
            allFrames: false,
            frameNumber: 0,
            indexInFrame: 0);
        if (Document is null || (uint)index >= (uint)Document.ByteMap.Count)
        {
            return;
        }

        ByteSpan span = Document.ByteMap[index];
        _keepHighlight = true;
        CodeIndex = span.FileIndex;
        _keepHighlight = false;
        HighlightStart = span.Start;
        HighlightLength = span.Length;
    }

    public void Schedule()
    {
        FontRequest request = Capture();
        _scheduler.Schedule(
            token => Compute(request, token),
            result => Apply((FontResult)result!),
            OnPreviewError);
    }

    [RelayCommand]
    private async Task OpenSheet()
    {
        string? path = _files.PickOpenSheet(_fontFolder);
        if (path is null)
        {
            return;
        }

        Activated?.Invoke();
        try
        {
            DecodedImage decoded = await Task.Run(() => _decoder.Decode(path)).ConfigureAwait(true);
            if (decoded.Frames.Count == 0)
            {
                _dialogs.Alert(_loc.Get("Error.Image.Unsupported"));
                return;
            }

            _fontFolder = Path.GetDirectoryName(path);
            LoadSheet(decoded.Frames[0], path);
        }
        catch (ImageLoadException ex)
        {
            _dialogs.Alert(UserText.ImageLoad(_loc, ex));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _dialogs.Alert(_loc.Format("Error.Unexpected", ex.Message));
        }
    }

    [RelayCommand]
    private async Task OpenImport()
    {
        string? path = _files.PickOpenImport(_fontFolder);
        if (path is null)
        {
            return;
        }

        Activated?.Invoke();
        try
        {
            IReadOnlyList<ImportedArray> arrays = await Task.Run(() => ReadArrays(path)).ConfigureAwait(true);
            _fontFolder = Path.GetDirectoryName(path);
            ApplyParsedImport(path, arrays);
        }
        catch (ArrayImportException ex)
        {
            _dialogs.Alert(UserText.Import(_loc, ex));
        }
        catch (ArgumentException)
        {
            _dialogs.Alert(_loc.Get("Error.Import.Extension"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _dialogs.Alert(_loc.Format("Error.Unexpected", ex.Message));
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        _history.Undo();
        AfterEdit();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        _history.Redo();
        AfterEdit();
    }

    [RelayCommand(CanExecute = nameof(CanCopy))]
    private void Copy()
    {
        if (CanCopy)
        {
            _clipboard.SetText(CodeText);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveOutput))]
    private void SaveOutput()
    {
        if (Document is null || !CanSaveOutput)
        {
            return;
        }

        string? directory = _files.PickFolder(_outputFolder, _loc.Get("File.OutputFolder"));
        if (directory is null)
        {
            return;
        }

        var blocked = new List<string>();
        if (!string.IsNullOrWhiteSpace(_sheetPath))
        {
            blocked.Add(_sheetPath);
        }

        if (!string.IsNullOrWhiteSpace(_importPath))
        {
            blocked.Add(_importPath);
        }

        try
        {
            OutputSaveResult result = OutputWriter.Save(
                Document,
                directory,
                existing => _dialogs.Confirm(_loc.Format("Dialog.Overwrite", string.Join(Environment.NewLine, existing))),
                blocked);
            if (result == OutputSaveResult.Saved)
            {
                _outputFolder = directory;
                FileUsed?.Invoke(directory, SessionFolder.SaveOutput);
            }
        }
        catch (OutputWriteException ex)
        {
            _dialogs.Alert(UserText.OutputWrite(_loc, ex));
        }
    }

    [RelayCommand(CanExecute = nameof(CanResetManual))]
    private void ResetManual()
    {
        if (!_table.HasManualEdits)
        {
            return;
        }

        if (!_dialogs.Confirm(_loc.Get("Dialog.ResetManual")))
        {
            return;
        }

        _table.ResetAllManual();
        _history.Clear();
        NotifyUndo();
        EditsChanged?.Invoke();
        TouchTable();
        Schedule();
    }

    [RelayCommand]
    private void SavePreset()
    {
        string? name = _dialogs.AskText(_loc.Get("Dialog.PresetName"), string.Empty);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            var preset = new Preset(name, controller: null, _cell.Width, _cell.Height, ToPacking(), _scheme, isBuiltIn: false);
            _users.Add(preset);
            _binding = PresetBinding.ForPreset(preset);
            Presets.Show(_binding);
            OnPropertyChanged(nameof(PresetCaption));
            ParametersChanged?.Invoke();
            PresetsChanged?.Invoke();
        }
        catch (PresetException ex)
        {
            _dialogs.Alert(UserText.Preset(_loc, ex));
        }
    }

    [RelayCommand(CanExecute = nameof(CanRenamePreset))]
    private void RenamePreset()
    {
        Preset? preset = Presets.Selected?.Preset;
        if (preset is null || !Presets.CanRename)
        {
            return;
        }

        string? name = _dialogs.AskText(_loc.Get("Dialog.RenamePreset"), preset.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            _users.Rename(preset.Name, name);
            if (string.Equals(_binding.Name, preset.Name, StringComparison.OrdinalIgnoreCase))
            {
                _binding = PresetBinding.ForPreset(_users.Find(name)!);
            }
            else if (string.Equals(_binding.BasedOn, preset.Name, StringComparison.OrdinalIgnoreCase))
            {
                _binding = new PresetBinding { BasedOn = name.Trim() };
            }

            Presets.Show(_binding);
            OnPropertyChanged(nameof(PresetCaption));
            ParametersChanged?.Invoke();
            PresetsChanged?.Invoke();
        }
        catch (PresetException ex)
        {
            _dialogs.Alert(UserText.Preset(_loc, ex));
        }
    }

    [RelayCommand(CanExecute = nameof(CanRenamePreset))]
    private void DeletePreset()
    {
        Preset? preset = Presets.Selected?.Preset;
        if (preset is null || !Presets.CanRename)
        {
            return;
        }

        if (!_dialogs.Confirm(_loc.Format("Dialog.DeletePreset", preset.Name)))
        {
            return;
        }

        try
        {
            _users.Remove(preset.Name);
            if (string.Equals(_binding.Name, preset.Name, StringComparison.OrdinalIgnoreCase))
            {
                _binding = PresetBinding.Custom;
            }

            Presets.Show(_binding);
            OnPropertyChanged(nameof(PresetCaption));
            ParametersChanged?.Invoke();
            PresetsChanged?.Invoke();
        }
        catch (PresetException ex)
        {
            _dialogs.Alert(UserText.Preset(_loc, ex));
        }
    }

    private bool CanRenamePreset() => Presets.CanRename;

    private void SetManual(int code, MonoBitmap? bitmap)
    {
        GlyphEditAction? action = GlyphEditAction.Apply(_table, code, bitmap);
        if (action is null)
        {
            return;
        }

        _history.Push(action);
        AfterEdit();
    }

    private void AfterEdit()
    {
        NotifyUndo();
        EditsChanged?.Invoke();
        TouchTable();
        Schedule();
    }

    private void ChoosePreset(Preset? preset)
    {
        FontTabParameters next = PresetApplication.Apply(ToParameters(), preset);
        ApplyParameters(next, RenderIntent.KeepPending);
        ParametersChanged?.Invoke();
        Schedule();
    }

    private void ApplyParameters(FontTabParameters parameters, RenderIntent intent)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        _cell = parameters.Cell;
        _sourceKind = parameters.SourceKind;
        _family = parameters.Family;
        _fontSize = parameters.FontSizePx;
        _fontSizeText = IntText.Format(_fontSize);
        _bold = parameters.Bold;
        _italic = parameters.Italic;
        _offsetX = parameters.GlyphOffsetX;
        _offsetY = parameters.GlyphOffsetY;
        _offsetXText = IntText.Format(_offsetX);
        _offsetYText = IntText.Format(_offsetY);
        _renderMode = parameters.RenderMode;
        _glyphThreshold = parameters.GlyphThreshold;
        _glyphThresholdText = IntText.Format(_glyphThreshold);
        SheetOptions sheet = parameters.Sheet;
        _sheetWidth = sheet.CellWidth;
        _sheetHeight = sheet.CellHeight;
        _sheetWidthText = IntText.Format(_sheetWidth);
        _sheetHeightText = IntText.Format(_sheetHeight);
        _marginX = sheet.MarginX;
        _marginY = sheet.MarginY;
        _spacingX = sheet.SpacingX;
        _spacingY = sheet.SpacingY;
        _marginXText = IntText.Format(_marginX);
        _marginYText = IntText.Format(_marginY);
        _spacingXText = IntText.Format(_spacingX);
        _spacingYText = IntText.Format(_spacingY);
        _charsPerRow = sheet.CharsPerRow;
        _charsPerRowText = IntText.Format(_charsPerRow);
        _firstCode = sheet.FirstCode;
        _firstCodeText = IntText.Format(_firstCode);
        _sheetThreshold = sheet.Threshold;
        _sheetThresholdText = IntText.Format(_sheetThreshold);
        _ranges = parameters.RangePresets;
        _customCodes = parameters.CustomCodes.OrderBy(code => code).ToArray();
        _customRangeText = FormatCodes(_customCodes);
        _previewText = parameters.PreviewText;
        _previewScale = parameters.PreviewScale;
        PackingOptions packing = parameters.Packing;
        _direction = packing.Direction;
        _bitOrder = packing.BitOrder;
        _bits = packing.BitsPerByte;
        _traversal = packing.PageTraversal;
        _invert = packing.Invert;
        _scheme = parameters.ColorScheme;
        _binding = parameters.Preset;
        OutputOptions output = parameters.Output;
        _format = output.Format;
        _arrayName = output.ArrayName;
        _encoding = output.Encoding;
        _bytesPerLine = output.BytesPerLine;
        _bytesText = IntText.Format(_bytesPerLine);
        _asmNumbers = output.AsmNumberFormat;
        _asmExtension = output.AsmFileExtension;
        _stm32 = output.Stm32ElementType;
        _includeDate = output.IncludeDate;
        _importArrayName = parameters.ImportArrayName;
        _nameCustomized = !IsAutomatic(_arrayName);
        if (!_nameCustomized)
        {
            UpdateAutomaticName();
        }

        if (intent == RenderIntent.Drop)
        {
            _job = SourceJob.None;
        }
        else if (intent == RenderIntent.RenderIfPossible && JobForCurrentKind() == SourceJob.Render)
        {
            _jobId++;
            _job = SourceJob.Render;
        }

        EnsureFamilyListed(_family);
        Presets.Show(_binding);
        OnPropertyChanged(string.Empty);
        ValidateAllProperties();
        NotifyDerived();
    }

    private void ApplyParsedImport(string path, IReadOnlyList<ImportedArray> arrays)
    {
        if (arrays.Count == 0)
        {
            _dialogs.Alert(_loc.Get("Import.None"));
            return;
        }

        ImportedArray array;
        FontCellSize cell = _cell;
        PackingOptions packing = ToPacking();
        if (arrays.Count == 1)
        {
            array = arrays[0];
        }
        else
        {
            ImportPick? pick = _dialogs.AskImport(arrays, _cell, packing);
            if (pick is null || (uint)pick.ArrayIndex >= (uint)arrays.Count)
            {
                return;
            }

            array = arrays[pick.ArrayIndex];
            cell = pick.Cell;
            packing = pick.Packing;
        }

        if (array.Error is ArrayImportIssue issue)
        {
            _dialogs.Alert(UserText.ImportIssue(_loc, issue));
            return;
        }

        if (_table.HasManualEdits && !_dialogs.Confirm(_loc.Get("Dialog.ReplaceFont")))
        {
            return;
        }

        bool packingChanged = packing != ToPacking();
        if (cell != _cell)
        {
            FollowSheetCell(_cell, cell);
            _cell = cell;
            _table = new FontTable(_cell);
        }
        else
        {
            _table.ResetAllManual();
        }

        _direction = packing.Direction;
        _bitOrder = packing.BitOrder;
        _bits = packing.BitsPerByte is 6 or 8 ? packing.BitsPerByte : 8;
        _traversal = packing.PageTraversal;
        _invert = packing.Invert;
        _history.Clear();
        _importBytes = array.Values.ToArray();
        _importPath = path;
        _importArrayName = array.Name;
        _sourceKind = FontSourceKind.Import;
        NotifyUndo();
        EditsChanged?.Invoke();
        OnPropertyChanged(string.Empty);
        TouchTable();
        FileUsed?.Invoke(path, SessionFolder.OpenFont);
        Commit(packingChanged, source: true, kindChanged: true);
    }

    private static IReadOnlyList<ImportedArray> ReadArrays(string path)
    {
        DecodedText text = TextFileReader.Read(path);
        return ArrayImportParser.Parse(text.Text, TextFileReader.GetSyntax(path));
    }

    private FontRequest Capture()
    {
        string? error = FirstError();
        if (error is not null)
        {
            return new FontRequest { Blocked = true, Message = error };
        }

        return new FontRequest
        {
            Job = _job,
            JobId = _jobId,
            Kind = _sourceKind,
            Cell = _cell,
            Family = _family.Trim(),
            FontSize = _fontSize,
            Bold = _bold,
            Italic = _italic,
            OffsetX = _offsetX,
            OffsetY = _offsetY,
            RenderMode = _renderMode,
            GlyphThreshold = _glyphThreshold,
            Sheet = _sheet,
            SheetPath = _sheetPath,
            SheetOptions = ToSheet(),
            ImportPath = _importPath,
            ImportName = _importArrayName,
            ImportBytes = _importBytes,
            Ranges = new CharRangeSet(_ranges, _customCodes),
            Packing = ToPacking(),
            Output = ToOutput(),
            Preset = _binding.ToInfo(FindPreset),
            Source = Describe(),
            Table = _table.Clone(),
        };
    }

    private FontResult Compute(FontRequest request, CancellationToken token)
    {
        if (request.Blocked)
        {
            return new FontResult { Blocked = true, Message = request.Message };
        }

        try
        {
            GlyphSet? glyphs = null;
            IReadOnlyList<int> missing = Array.Empty<int>();
            IReadOnlyList<Diagnostic> diagnostics = Array.Empty<Diagnostic>();
            if (request.Job == SourceJob.Clear)
            {
                glyphs = new GlyphSet(request.Cell);
            }
            else if (request.Job == SourceJob.Render)
            {
                GlyphSourceResult rendered = RenderSource(request, token);
                glyphs = rendered.Glyphs;
                missing = rendered.MissingCodes;
                diagnostics = rendered.Diagnostics;
            }

            if (glyphs is not null)
            {
                request.Table.ReplaceSource(glyphs);
            }

            token.ThrowIfCancellationRequested();
            IPacker packer = _packers.Get(request.Packing);
            FontOutputData data = request.Table.ToOutputData(packer, request.Packing, request.Source, request.Preset);
            OutputDocument document = _generators.Generate(data, request.Output);
            return new FontResult
            {
                Job = request.Job,
                JobId = request.JobId,
                Glyphs = glyphs,
                Missing = missing,
                SourceDiagnostics = diagnostics,
                Document = document,
                Bytes = data.GetAllBytes(),
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new FontResult { Blocked = true, Message = _loc.Format("Error.Unexpected", ex.Message) };
        }
    }

    private GlyphSourceResult RenderSource(FontRequest request, CancellationToken token)
    {
        IGlyphSource source = request.Kind switch
        {
            FontSourceKind.TrueType => new TrueTypeGlyphSource(_cache, new TrueTypeOptions(
                new FontFaceSpec(request.Family, request.FontSize, request.Bold, request.Italic))
            {
                OffsetX = request.OffsetX,
                OffsetY = request.OffsetY,
                RenderMode = request.RenderMode,
                Threshold = request.GlyphThreshold,
            }),
            FontSourceKind.Sheet => new SheetGlyphSource(request.Sheet!, request.SheetPath ?? "sheet", request.SheetOptions),
            FontSourceKind.Import => new ImportGlyphSource(
                new ImportedArray(request.ImportName, ImportedArrayKind.CInitializer, 1, request.ImportBytes ?? Array.Empty<byte>(), null),
                request.ImportPath ?? "font.c",
                _packers.Get(request.Packing),
                request.Packing),
            _ => throw new InvalidOperationException("There is no glyph source to render."),
        };
        return source.Render(request.Cell, request.Ranges, token);
    }

    private void Apply(FontResult result)
    {
        if (result.Blocked)
        {
            Document = null;
            CodeFiles.Clear();
            _codeIndex = -1;
            CodeIndex = 0;
            CodeText = result.Message ?? string.Empty;
            HighlightStart = 0;
            HighlightLength = 0;
            _fontBytes = null;
            WarningText = string.Empty;
            RefreshCommands();
            CommandsChanged?.Invoke();
            return;
        }

        if (result.Glyphs is not null && result.Glyphs.Cell == _table.Cell)
        {
            _table.ReplaceSource(result.Glyphs);
            _missing = result.Missing;
            _sourceDiagnostics = result.SourceDiagnostics;
            if (_jobId == result.JobId)
            {
                _job = SourceJob.None;
            }

            TouchTable();
        }

        _fontBytes = result.Bytes;
        Document = result.Document;
        if (result.Document is not null)
        {
            FillCode(result.Document);
        }

        WarningText = string.Join(
            Environment.NewLine,
            _sourceDiagnostics.Concat(result.Document?.Diagnostics ?? Array.Empty<Diagnostic>())
                .Select(item => UserText.Diagnostic(_loc, item)));
        OnPropertyChanged(nameof(MissingText));
        OnPropertyChanged(nameof(HasMissing));
        OnPropertyChanged(nameof(SizeLine));
        OnPropertyChanged(nameof(Document));
        RefreshCommands();
        CommandsChanged?.Invoke();
    }

    private void OnPreviewError(Exception exception) =>
        _dialogs.Alert(_loc.Format("Error.Unexpected", exception.Message));

    private void FillCode(OutputDocument document)
    {
        CodeFiles.Clear();
        foreach (OutputFile file in document.Files)
        {
            CodeFiles.Add(new CodeFileItem(file.FileName, file.Text));
        }

        _keepHighlight = true;
        _codeIndex = -1;
        CodeIndex = document.ByteMap.FileIndex;
        _keepHighlight = false;
        HighlightStart = 0;
        HighlightLength = 0;
    }

    private void Commit(bool preset, bool source, bool kindChanged = false)
    {
        if (_suppress)
        {
            return;
        }

        if (preset)
        {
            MarkPreset();
        }

        if (source)
        {
            SourceJob next = JobForCurrentKind();
            if (next == SourceJob.None && kindChanged)
            {
                next = SourceJob.Clear;
            }

            if (next != SourceJob.None)
            {
                _jobId++;
                _job = next;
            }
        }

        UpdateAutomaticName();
        NotifyDerived();
        ParametersChanged?.Invoke();
        Schedule();
    }

    private void MarkPreset()
    {
        PresetBinding next = _binding.Edited();
        if (next.Equals(_binding))
        {
            return;
        }

        _binding = next;
        Presets.Show(_binding);
        OnPropertyChanged(nameof(PresetCaption));
    }

    private SourceJob JobForCurrentKind() => _sourceKind switch
    {
        FontSourceKind.TrueType when !string.IsNullOrWhiteSpace(_family) => SourceJob.Render,
        FontSourceKind.Sheet when _sheet is not null => SourceJob.Render,
        FontSourceKind.Import when _importBytes is not null => SourceJob.Render,
        FontSourceKind.Manual => SourceJob.Clear,
        _ => SourceJob.None,
    };

    private void FollowSheetCell(FontCellSize previous, FontCellSize next)
    {
        if (_sheetWidth != previous.Width || _sheetHeight != previous.Height)
        {
            return;
        }

        _sheetWidth = next.Width;
        _sheetHeight = next.Height;
        _sheetWidthText = IntText.Format(next.Width);
        _sheetHeightText = IntText.Format(next.Height);
        OnPropertyChanged(nameof(SheetWidthText));
        OnPropertyChanged(nameof(SheetHeightText));
    }

    private void UpdateAutomaticName()
    {
        if (_nameCustomized)
        {
            return;
        }

        string name = BuildAutoName();
        if (name == _arrayName)
        {
            return;
        }

        _arrayName = name;
        OnPropertyChanged(nameof(ArrayName));
        ValidateProperty(_arrayName, nameof(ArrayName));
    }

    private bool IsAutomatic(string name) =>
        string.Equals(name, "font", StringComparison.Ordinal)
        || string.Equals(name, BuildAutoName(), StringComparison.Ordinal);

    private string BuildAutoName()
    {
        string? basis = _sourceKind switch
        {
            FontSourceKind.TrueType => string.IsNullOrWhiteSpace(_family) ? null : _family.Trim(),
            FontSourceKind.Sheet => _sheetPath,
            FontSourceKind.Import => _importPath,
            _ => null,
        };
        if (string.IsNullOrWhiteSpace(basis))
        {
            return DefaultNameBuilder.Build(null, _cell.Width, _cell.Height, ArrayNameKind.Font);
        }

        bool file = basis.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || basis.Contains('/', StringComparison.Ordinal)
            || basis.Contains('\\', StringComparison.Ordinal)
            || Path.HasExtension(basis);
        return file
            ? DefaultNameBuilder.FromFile(basis, _cell.Width, _cell.Height, ArrayNameKind.Font)
            : DefaultNameBuilder.Build(basis, _cell.Width, _cell.Height, ArrayNameKind.Font);
    }

    private bool BeginChange<T>(ref T field, T value, bool source, bool preset, bool kindChanged = false, Action? before = null, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        before?.Invoke();
        if (_suppress)
        {
            return false;
        }

        Commit(preset, source, kindChanged);
        return true;
    }

    private void SetNumber(ref string field, string? value, ref int stored, int min, int max, bool source, bool preset, [CallerMemberName] string? propertyName = null)
    {
        value ??= string.Empty;
        if (!SetProperty(ref field, value, true, propertyName ?? string.Empty))
        {
            return;
        }

        if (_suppress)
        {
            return;
        }

        if (!IntText.TryParse(value, min, max, out int parsed))
        {
            RefreshCommands();
            Schedule();
            return;
        }

        if (parsed == stored)
        {
            Unblock();
            return;
        }

        stored = parsed;
        Commit(preset, source);
    }

    /// <summary>Поле вернулось к прежнему допустимому значению: снять блокировку, если других ошибок нет.</summary>
    private void Unblock()
    {
        RefreshCommands();
        Schedule();
    }

    private void SetFlag(CharRangePreset flag, bool on)
    {
        CharRangePreset next = on ? _ranges | flag : _ranges & ~flag;
        if (next == _ranges)
        {
            return;
        }

        _ranges = next;
        OnPropertyChanged(nameof(Latin));
        OnPropertyChanged(nameof(Cyrillic));
        OnPropertyChanged(nameof(OtherRange));
        if (!_suppress)
        {
            Commit(preset: false, source: true);
        }
    }

    private SheetOptions ToSheet() => new()
    {
        CellWidth = _sheetWidth,
        CellHeight = _sheetHeight,
        MarginX = _marginX,
        MarginY = _marginY,
        SpacingX = _spacingX,
        SpacingY = _spacingY,
        CharsPerRow = _charsPerRow,
        FirstCode = _firstCode,
        Threshold = _sheetThreshold,
    };

    private PackingOptions ToPacking() => new()
    {
        Direction = _direction,
        BitOrder = _bitOrder,
        BitsPerByte = _bits,
        PageTraversal = _traversal,
        Invert = _invert,
    };

    private OutputOptions ToOutput() => new()
    {
        Format = _format,
        ArrayName = _arrayName,
        Encoding = _encoding,
        BytesPerLine = _bytesPerLine,
        AsmNumberFormat = _asmNumbers,
        AsmFileExtension = _asmExtension,
        Stm32ElementType = _stm32,
        IncludeDate = _includeDate,
    };

    private FontSourceInfo Describe() => _sourceKind switch
    {
        FontSourceKind.TrueType when !string.IsNullOrWhiteSpace(_family) =>
            FontSourceInfo.TrueType(_family.Trim(), _fontSize, _bold, _italic),
        FontSourceKind.Sheet when !string.IsNullOrWhiteSpace(_sheetPath) => FontSourceInfo.Sheet(_sheetPath),
        FontSourceKind.Import when !string.IsNullOrWhiteSpace(_importPath) =>
            FontSourceInfo.Import(_importPath, _importArrayName),
        _ => FontSourceInfo.Manual,
    };

    private void TouchTable()
    {
        _tableRevision++;
        OnPropertyChanged(nameof(Table));
        OnPropertyChanged(nameof(TableRevision));
        Editor.Reload();
        OnPropertyChanged(nameof(SizeLine));
    }

    private void NotifyDerived()
    {
        OnPropertyChanged(nameof(IsTrueType));
        OnPropertyChanged(nameof(IsSheet));
        OnPropertyChanged(nameof(IsImport));
        OnPropertyChanged(nameof(ShowRanges));
        OnPropertyChanged(nameof(ShowGlyphThreshold));
        OnPropertyChanged(nameof(BitsEnabled));
        OnPropertyChanged(nameof(TraversalEnabled));
        OnPropertyChanged(nameof(AsmEnabled));
        OnPropertyChanged(nameof(Stm32Enabled));
        OnPropertyChanged(nameof(PresetCaption));
        OnPropertyChanged(nameof(SheetCaption));
        OnPropertyChanged(nameof(ImportCaption));
        OnPropertyChanged(nameof(SizeLine));
        OnPropertyChanged(nameof(ShowCodeTabs));
        OnPropertyChanged(nameof(MissingText));
        OnPropertyChanged(nameof(HasMissing));
    }

    private void NotifyUndo()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(CanResetManual));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        ResetManualCommand.NotifyCanExecuteChanged();
        CommandsChanged?.Invoke();
    }

    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(CanCopy));
        OnPropertyChanged(nameof(CanSaveOutput));
        CopyCommand.NotifyCanExecuteChanged();
        SaveOutputCommand.NotifyCanExecuteChanged();
    }

    private string? FirstError()
    {
        foreach (object? error in GetErrors(null))
        {
            if (error is ValidationResult result && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                return result.ErrorMessage;
            }

            if (error is string text && text.Length > 0)
            {
                return text;
            }
        }

        return null;
    }

    private bool HasSourceGlyph()
    {
        for (int code = 0; code < FontTable.CharCount; code++)
        {
            if (_table.GetOrigin(code) == GlyphOrigin.Source)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureFamilyListed(string family)
    {
        if (string.IsNullOrWhiteSpace(family))
        {
            return;
        }

        if (Families.Any(item => string.Equals(item, family, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        Families.Insert(0, family);
    }

    private string FormatCode(int code)
    {
        string hex = "0x" + code.ToString("X2", CultureInfo.InvariantCulture);
        string character = Cp1251.GetString(new[] { (byte)code });
        if (character.Length == 1 && !char.IsControl(character[0]))
        {
            return _loc.Format("Font.Code", hex, character);
        }

        return hex;
    }

    private static string FormatCodes(IReadOnlyList<int> codes) =>
        string.Join(", ", codes.Select(code => "0x" + code.ToString("X2", CultureInfo.InvariantCulture)));

    private Preset? FindPreset(string name) => _catalog.Find(name) ?? _users.Find(name);

    private bool NumberApplies(string? member) => member switch
    {
        nameof(FontSizeText) or nameof(OffsetXText) or nameof(OffsetYText) or nameof(GlyphThresholdText) =>
            _sourceKind == FontSourceKind.TrueType,
        nameof(SheetWidthText) or nameof(SheetHeightText) or nameof(MarginXText) or nameof(MarginYText)
            or nameof(SpacingXText) or nameof(SpacingYText) or nameof(CharsPerRowText) or nameof(FirstCodeText)
            or nameof(SheetThresholdText) => _sourceKind == FontSourceKind.Sheet,
        _ => true,
    };

    private sealed class FontRequest
    {
        public bool Blocked { get; init; }

        public string? Message { get; init; }

        public SourceJob Job { get; init; }

        public int JobId { get; init; }

        public FontSourceKind Kind { get; init; }

        public FontCellSize Cell { get; init; } = FontCellSize.Cell6x8;

        public string Family { get; init; } = string.Empty;

        public int FontSize { get; init; }

        public bool Bold { get; init; }

        public bool Italic { get; init; }

        public int OffsetX { get; init; }

        public int OffsetY { get; init; }

        public GlyphRenderMode RenderMode { get; init; }

        public int GlyphThreshold { get; init; }

        public RgbaImage? Sheet { get; init; }

        public string? SheetPath { get; init; }

        public SheetOptions SheetOptions { get; init; } = new();

        public string? ImportPath { get; init; }

        public string? ImportName { get; init; }

        public byte[]? ImportBytes { get; init; }

        public CharRangeSet Ranges { get; init; } = CharRangeSet.Default;

        public PackingOptions Packing { get; init; } = PackingOptions.Default;

        public OutputOptions Output { get; init; } = OutputOptions.Default;

        public PresetInfo Preset { get; init; } = PresetInfo.Custom;

        public FontSourceInfo Source { get; init; } = FontSourceInfo.Manual;

        public FontTable Table { get; init; } = new(FontCellSize.Cell6x8);
    }

    private sealed class FontResult
    {
        public bool Blocked { get; init; }

        public string? Message { get; init; }

        public SourceJob Job { get; init; }

        public int JobId { get; init; }

        public GlyphSet? Glyphs { get; init; }

        public IReadOnlyList<int> Missing { get; init; } = Array.Empty<int>();

        public IReadOnlyList<Diagnostic> SourceDiagnostics { get; init; } = Array.Empty<Diagnostic>();

        public OutputDocument? Document { get; init; }

        public byte[]? Bytes { get; init; }
    }
}
