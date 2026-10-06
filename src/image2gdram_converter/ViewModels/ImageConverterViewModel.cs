using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Image2Gdram.Core.Editing;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Settings;
using Image2Gdram.Core.Text;
using image2gdram_converter.Services;

namespace image2gdram_converter.ViewModels;

/// <summary>  : , ,    .</summary>
public sealed partial class ImageConverterViewModel : ObservableValidator
{
    private readonly ILocalizationService _loc;
    private readonly IDialogService _dialogs;
    private readonly IFileDialogService _files;
    private readonly IClipboardService _clipboard;
    private readonly IRecalcScheduler _scheduler;
    private readonly IImageDecoder _decoder;
    private readonly PresetCatalog _catalog;
    private readonly UserPresetStore _users;
    private readonly PackerRegistry _packers = PackerRegistry.CreateDefault();
    private readonly OutputGeneratorRegistry _generators = OutputGeneratorRegistry.CreateDefault();
    private readonly ImagePipeline _pipeline = new();
    private readonly EditHistory _history = new();

    private DecodedImage? _image;
    private string? _sourcePath;
    private FramePixelOverrides _edits = new();
    private bool _suppress;
    private bool _nameCustomized;
    private bool _busy;
    private bool _keepHighlight;
    private string? _openFolder;
    private string? _outputFolder;

    private BackgroundColor _background = BackgroundColor.White;
    private Rotation _rotation = Rotation.Rotate0;
    private bool _flipHorizontal;
    private bool _flipVertical;
    private TargetSizeMode _sizeMode = TargetSizeMode.Manual;
    private int _width = 128;
    private int _height = 64;
    private string _widthText = "128";
    private string _heightText = "64";
    private FitMode _fit = FitMode.Fit;
    private Alignment _alignment = Alignment.Center;
    private int _offsetX;
    private int _offsetY;
    private string _offsetXText = "0";
    private string _offsetYText = "0";
    private ResampleMode _resample = ResampleMode.AreaAverage;
    private BinarizeMode _binarize = BinarizeMode.Threshold;
    private int _threshold = ProcessingOptions.DefaultThreshold;
    private string _thresholdText = "128";

    private PackDirection _direction = PackDirection.Vertical;
    private BitOrder _bitOrder = BitOrder.LsbFirst;
    private int _bits = 8;
    private PageTraversal _traversal = PageTraversal.ByPages;
    private bool _invert;
    private ColorScheme _scheme = ColorScheme.Oled;
    private PresetBinding _binding = PresetBinding.Custom;

    private OutputFormat _format = OutputFormat.CKeilC51;
    private string _arrayName = "image";
    private OutputEncoding _encoding = OutputEncoding.Cp1251;
    private int _bytesPerLine = 16;
    private string _bytesText = "16";
    private AsmNumberFormat _asmNumbers = AsmNumberFormat.Hex;
    private AsmFileExtension _asmExtension = AsmFileExtension.A51;
    private Stm32ElementType _stm32 = Stm32ElementType.Uint8T;
    private bool _includeDate = true;

    private int _selectedFrame = 1;
    private bool _exportAll;
    private bool _showGrid = true;
    private bool _fitWindow = true;
    private int _scale = 8;
    private ZoomChoice? _zoom;

    private byte[]? _frameBytes;
    private int _frameSize;
    private bool _packedExportAll;
    private int _packedFrameNumber = 1;
    private int _codeIndex;
    private string _codeText = string.Empty;
    private string _hoverText = string.Empty;
    private string _warningText = string.Empty;
    private int _highlightStart;
    private int _highlightLength;

    public ImageConverterViewModel(
        ILocalizationService localization,
        IDialogService dialogs,
        IFileDialogService files,
        IClipboardService clipboard,
        IRecalcScheduler scheduler,
        IImageDecoder decoder,
        PresetCatalog catalog,
        UserPresetStore userPresets,
        ImageTabParameters initial)
    {
        _loc = localization;
        _dialogs = dialogs;
        _files = files;
        _clipboard = clipboard;
        _scheduler = scheduler;
        _decoder = decoder;
        _catalog = catalog;
        _users = userPresets;
        Backgrounds = OptionLists.Backgrounds(localization);
        Rotations = OptionLists.Rotations(localization);
        SizeModes = OptionLists.SizeModes(localization);
        Fits = OptionLists.Fits(localization);
        Alignments = OptionLists.Alignments(localization);
        Resamples = OptionLists.Resamples(localization);
        BinarizeModes = OptionLists.BinarizeModes(localization);
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
        Zooms = OptionLists.Zooms(localization);
        _zoom = Zooms[0];
        Presets = new PresetSelectorViewModel(localization, catalog, userPresets, ChoosePreset);
        Presets.SelectionChanged += () =>
        {
            RenamePresetCommand.NotifyCanExecuteChanged();
            DeletePresetCommand.NotifyCanExecuteChanged();
        };
        LoadState(initial, schedule: true);
    }

    public PresetSelectorViewModel Presets { get; }

    public Action? ParametersChanged { get; set; }

    public Action? EditsChanged { get; set; }

    public Action<string, SessionFolder>? FileUsed { get; set; }

    public Action? CommandsChanged { get; set; }

    public ObservableCollection<CodeFileItem> CodeFiles { get; } = new();

    public ObservableCollection<int> Frames { get; } = new();

    public IReadOnlyList<Labeled<BackgroundColor>> Backgrounds { get; }

    public IReadOnlyList<Labeled<Rotation>> Rotations { get; }

    public IReadOnlyList<Labeled<TargetSizeMode>> SizeModes { get; }

    public IReadOnlyList<Labeled<FitMode>> Fits { get; }

    public IReadOnlyList<Labeled<Alignment>> Alignments { get; }

    public IReadOnlyList<Labeled<ResampleMode>> Resamples { get; }

    public IReadOnlyList<Labeled<BinarizeMode>> BinarizeModes { get; }

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

    public IReadOnlyList<ZoomChoice> Zooms { get; }

    public MonoBitmap? Preview { get; private set; }

    public RgbaImage? SourcePreview { get; private set; }

    public OutputDocument? Document { get; private set; }

    public string? SourcePath => _sourcePath;

    public FramePixelOverrides Edits => _edits;

    public bool HasEdits => _edits.HasEdits;

    public bool HasImage => _image is not null;

    public bool IsAnimated => _image?.IsAnimated == true;

    public string SourceName => string.IsNullOrWhiteSpace(_sourcePath) ? string.Empty : Path.GetFileName(_sourcePath);

    public string PresetCaption => Presets.Caption;

    public bool ManualSize => _sizeMode == TargetSizeMode.Manual;

    public bool BitsEnabled => _direction == PackDirection.Horizontal;

    public bool TraversalEnabled => _direction == PackDirection.Vertical && EffectiveHeight() > 8;

    public bool AsmEnabled => _format is OutputFormat.A51Module or OutputFormat.A51Include;

    public bool Stm32Enabled => _format == OutputFormat.CStm32;

    public bool ShowCodeTabs => CodeFiles.Count > 1;

    public bool CanUndo => _history.CanUndo;

    public bool CanRedo => _history.CanRedo;

    public bool CanCopy => Document is not null && FirstError() is null;

    public bool CanSaveOutput => CanCopy;

    public string ResultSizeText
    {
        get
        {
            if (Preview is null || _frameBytes is null)
            {
                return string.Empty;
            }

            int frames = _packedExportAll && _image is not null ? _image.Frames.Count : 1;
            return _loc.Format("Status.Result", Preview.Width, Preview.Height, RussianPlural.Bytes(_frameSize * frames));
        }
    }

    public string CodeText
    {
        get => _codeText;
        private set => SetProperty(ref _codeText, value);
    }

    public string HoverText
    {
        get => _hoverText;
        private set => SetProperty(ref _hoverText, value);
    }

    public string WarningText
    {
        get => _warningText;
        private set => SetProperty(ref _warningText, value);
    }

    public int HighlightStart
    {
        get => _highlightStart;
        private set => SetProperty(ref _highlightStart, value);
    }

    public int HighlightLength
    {
        get => _highlightLength;
        private set => SetProperty(ref _highlightLength, value);
    }

    public int CodeIndex
    {
        get => _codeIndex;
        set
        {
            if (_codeIndex == value)
            {
                return;
            }

            _codeIndex = value;
            OnPropertyChanged();
            if ((uint)value < (uint)CodeFiles.Count)
            {
                CodeText = CodeFiles[value].Text;
            }

            if (!_keepHighlight)
            {
                HighlightStart = 0;
                HighlightLength = 0;
            }
        }
    }

    public void SetFolders(string? openImage, string? saveOutput)
    {
        _openFolder = openImage;
        _outputFolder = saveOutput;
    }

    public void LoadState(ImageTabParameters parameters, bool schedule)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        _suppress = true;
        ProcessingOptions processing = parameters.Processing;
        _background = processing.Background;
        _rotation = processing.Rotation;
        _flipHorizontal = processing.FlipHorizontal;
        _flipVertical = processing.FlipVertical;
        _sizeMode = processing.SizeMode;
        _width = processing.TargetWidth;
        _height = processing.TargetHeight;
        _widthText = IntText.Format(_width);
        _heightText = IntText.Format(_height);
        _fit = processing.Fit;
        _alignment = processing.Alignment;
        _offsetX = processing.OffsetX;
        _offsetY = processing.OffsetY;
        _offsetXText = IntText.Format(_offsetX);
        _offsetYText = IntText.Format(_offsetY);
        _resample = processing.Resample;
        _binarize = processing.Binarize;
        _threshold = processing.Threshold;
        _thresholdText = IntText.Format(_threshold);

        PackingOptions packing = parameters.Packing;
        _direction = packing.Direction;
        _bitOrder = packing.BitOrder;
        _bits = packing.BitsPerByte is 6 or 8 ? packing.BitsPerByte : 8;
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
        _selectedFrame = Math.Max(1, parameters.SelectedFrame);
        _exportAll = parameters.ExportAllFrames;
        _nameCustomized = !IsAutomaticName(_arrayName);
        _suppress = false;

        Presets.Show(_binding);
        OnPropertyChanged(string.Empty);
        ValidateAllProperties();
        NotifyDerived();
        if (schedule)
        {
            Schedule();
        }
    }

    public void LoadDecoded(DecodedImage image, string path, bool resetFrame, bool userAction)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _image = image;
        _sourcePath = path;
        if (resetFrame || _selectedFrame < 1 || _selectedFrame > image.Frames.Count)
        {
            _selectedFrame = 1;
        }

        RebuildFrames();
        OnPropertyChanged(nameof(SelectedFrame));
        UpdateAutomaticName();
        if (userAction)
        {
            ParametersChanged?.Invoke();
            CommandsChanged?.Invoke();
        }

        Schedule();
    }

    public void UnloadPixels()
    {
        _image = null;
        RebuildFrames();
        Schedule();
    }

    public void ResetSession()
    {
        _image = null;
        _sourcePath = null;
        _edits.Clear();
        _history.Clear();
        _selectedFrame = 1;
        _exportAll = false;
        RebuildFrames();
        NotifyUndo();
        OnPropertyChanged(nameof(SelectedFrame));
        OnPropertyChanged(nameof(ExportAllFrames));
        Schedule();
    }

    public void ReplaceEdits(FramePixelOverrides edits)
    {
        _edits = edits ?? new FramePixelOverrides();
        _history.Clear();
        NotifyUndo();
    }

    public ImageTabParameters ToParameters() => new()
    {
        Processing = ToProcessing(),
        Packing = ToPacking(),
        Output = ToOutput(),
        ColorScheme = _scheme,
        Preset = _binding,
        SelectedFrame = _selectedFrame,
        ExportAllFrames = _exportAll,
    };

    public void ApplyStroke(IReadOnlyList<(int X, int Y)> points, StrokePaint paint)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (Preview is null || points.Count == 0 || FirstError() is not null)
        {
            return;
        }

        PixelOverrides frame = _edits.ForFrame(FrameIndex);
        PixelStrokeAction? action;
        if (paint == StrokePaint.Toggle)
        {
            (int x, int y) = points[0];
            if ((uint)x >= (uint)Preview.Width || (uint)y >= (uint)Preview.Height)
            {
                return;
            }

            bool current = frame.TryGet(x, y, out bool stored) ? stored : Preview[x, y];
            action = PixelStrokeAction.Apply(frame, new[] { (x, y) }, !current);
        }
        else
        {
            bool active = PixelPaint.Logical(_invert, paint);
            var inside = points.Where(point => (uint)point.X < (uint)Preview.Width && (uint)point.Y < (uint)Preview.Height);
            action = PixelStrokeAction.Apply(frame, inside, active);
        }

        if (action is null)
        {
            return;
        }

        _history.Push(action);
        NotifyUndo();
        EditsChanged?.Invoke();
        Schedule();
    }

    public void SetHover(int? x, int? y)
    {
        if (x is null || y is null || Preview is null || _frameBytes is null
            || (uint)x.Value >= (uint)Preview.Width || (uint)y.Value >= (uint)Preview.Height)
        {
            HoverText = string.Empty;
            HighlightLength = 0;
            return;
        }

        PackingOptions packing = ToPacking();
        BitLocation location = _packers.Get(packing).Locate(x.Value, y.Value, Preview.Width, Preview.Height, packing);
        int local = location.ByteIndex;
        if ((uint)local >= (uint)_frameBytes.Length)
        {
            return;
        }

        int full = _packedExportAll ? ((_packedFrameNumber - 1) * _frameSize) + local : local;
        HoverText = UserText.Hover(
            _loc,
            x.Value,
            y.Value,
            full,
            location.Bit,
            _frameBytes[local],
            Preview[x.Value, y.Value],
            _packedExportAll,
            _packedFrameNumber,
            local);

        if (Document is null || (uint)full >= (uint)Document.ByteMap.Count)
        {
            return;
        }

        ByteSpan span = Document.ByteMap[full];
        _keepHighlight = true;
        CodeIndex = span.FileIndex;
        _keepHighlight = false;
        HighlightStart = span.Start;
        HighlightLength = span.Length;
    }

    [RelayCommand(CanExecute = nameof(CanOpenImage))]
    private async Task OpenImageAsync()
    {
        string? path = _files.PickOpenImage(_openFolder);
        if (path is null)
        {
            return;
        }

        await LoadPathAsync(path).ConfigureAwait(true);
    }

    public Task OpenPath(string path) => LoadPathAsync(path);

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        _history.Undo();
        NotifyUndo();
        EditsChanged?.Invoke();
        Schedule();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        _history.Redo();
        NotifyUndo();
        EditsChanged?.Invoke();
        Schedule();
    }

    [RelayCommand(CanExecute = nameof(CanCopy))]
    private void Copy()
    {
        if (!CanCopy)
        {
            return;
        }

        _clipboard.SetText(CodeText);
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

        try
        {
            OutputSaveResult result = OutputWriter.Save(
                Document,
                directory,
                existing => _dialogs.Confirm(_loc.Format("Dialog.Overwrite", string.Join(Environment.NewLine, existing))),
                _sourcePath is null ? null : new[] { _sourcePath });
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
            var preset = new Preset(name, controller: null, _width, _height, ToPacking(), _scheme, isBuiltIn: false);
            _users.Add(preset);
            _binding = PresetBinding.ForPreset(preset);
            Presets.Show(_binding);
            OnPropertyChanged(nameof(PresetCaption));
            ParametersChanged?.Invoke();
        }
        catch (PresetException ex)
        {
            _dialogs.Alert(UserText.Preset(_loc, ex));
        }
    }

    [RelayCommand(CanExecute = nameof(CanRenamePreset))]
    private void RenamePreset()
    {
        if (Presets.Selected?.Preset is not { } preset || !Presets.CanRename)
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

            Presets.Show(_binding);
            OnPropertyChanged(nameof(PresetCaption));
            ParametersChanged?.Invoke();
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
        }
        catch (PresetException ex)
        {
            _dialogs.Alert(UserText.Preset(_loc, ex));
        }
    }

    public void Schedule()
    {
        PreviewRequest request = Capture();
        _scheduler.Schedule(
            token => Compute(request, token),
            result => Apply((PreviewResult)result!),
            OnPreviewError);
    }

    private bool CanOpenImage() => !_busy;

    private bool CanRenamePreset() => Presets.CanRename;

    private async Task LoadPathAsync(string path)
    {
        _busy = true;
        OpenImageCommand.NotifyCanExecuteChanged();
        try
        {
            DecodedImage decoded = await Task.Run(() => _decoder.Decode(path)).ConfigureAwait(true);
            if (HasEdits && !ConfirmStep())
            {
                return;
            }

            LoadDecoded(decoded, path, resetFrame: true, userAction: true);
            _openFolder = Path.GetDirectoryName(path);
            FileUsed?.Invoke(path, SessionFolder.OpenImage);
        }
        catch (ImageLoadException ex)
        {
            _dialogs.Alert(UserText.ImageLoad(_loc, ex));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _dialogs.Alert(_loc.Format("Error.Unexpected", ex.Message));
        }
        finally
        {
            _busy = false;
            OpenImageCommand.NotifyCanExecuteChanged();
        }
    }

    private void ChoosePreset(Preset? preset)
    {
        ImageTabParameters next = PresetApplication.Apply(ToParameters(), preset);
        bool sizeChanged = next.Processing.SizeMode != _sizeMode
            || next.Processing.TargetWidth != _width
            || next.Processing.TargetHeight != _height;
        if (sizeChanged && !ConfirmStep())
        {
            Presets.Show(_binding);
            return;
        }

        LoadState(next, schedule: true);
        ParametersChanged?.Invoke();
        CommandsChanged?.Invoke();
    }

    private PreviewRequest Capture()
    {
        string? error = FirstError();
        if (error is not null)
        {
            return new PreviewRequest { Kind = PreviewKind.KeepAndBlock, Message = error };
        }

        if (_image is null)
        {
            return new PreviewRequest { Kind = PreviewKind.Clear, Message = _loc.Get("Code.NoImage") };
        }

        return new PreviewRequest
        {
            Kind = PreviewKind.Run,
            Processing = ToProcessing(),
            Packing = ToPacking(),
            Output = ToOutput(),
            Preset = _binding.ToInfo(FindPreset),
            Image = _image,
            SourcePath = _sourcePath,
            FrameIndex = FrameIndex,
            ExportAll = _exportAll && _image.IsAnimated,
            Edits = CloneEdits(_edits),
        };
    }

    private PreviewResult Compute(PreviewRequest request, CancellationToken token)
    {
        if (request.Kind != PreviewKind.Run || request.Image is null)
        {
            return new PreviewResult { Kind = request.Kind, Message = request.Message };
        }

        try
        {
            int selected = Math.Clamp(request.FrameIndex, 0, request.Image.Frames.Count - 1);
            int count = request.ExportAll ? request.Image.Frames.Count : 1;
            var packed = new byte[count][];
            MonoBitmap? bitmap = null;
            RgbaImage? source = null;
            for (int i = 0; i < count; i++)
            {
                token.ThrowIfCancellationRequested();
                int index = request.ExportAll ? i : selected;
                RgbaImage frame = request.Image.Frames[index];
                MonoBitmap mono = _pipeline.Run(frame, request.Processing, request.Edits.ForFrame(index), token);
                packed[i] = _packers.Get(request.Packing).Pack(mono, request.Packing);
                if (index == selected)
                {
                    bitmap = mono;
                    source = BackgroundCompositor.Apply(frame, request.Processing.Background);
                }
            }

            if (bitmap is null)
            {
                return new PreviewResult { Kind = PreviewKind.Clear, Message = _loc.Get("Code.NoImage") };
            }

            var info = new ImageSourceInfo(
                string.IsNullOrWhiteSpace(request.SourcePath) ? "image" : request.SourcePath,
                request.Image.Frames.Count,
                selected + 1);
            OutputData data = request.ExportAll
                ? new ImageOutputData(bitmap.Width, bitmap.Height, packed, true, request.Packing, info, request.Preset)
                : new ImageOutputData(bitmap.Width, bitmap.Height, packed[0], request.Packing, info, request.Preset);
            OutputDocument document = _generators.Generate(data, request.Output);
            int shown = request.ExportAll ? selected : 0;
            return new PreviewResult
            {
                Kind = PreviewKind.Run,
                Bitmap = bitmap,
                Source = source,
                FrameBytes = packed[shown],
                FrameSize = packed[shown].Length,
                ExportAll = request.ExportAll,
                FrameNumber = selected + 1,
                Document = document,
                Diagnostics = document.Diagnostics,
                Width = bitmap.Width,
                Height = bitmap.Height,
            };
        }
        catch (PipelineException ex)
        {
            return new PreviewResult
            {
                Kind = PreviewKind.Clear,
                Message = _loc.Format("Error.Pipeline.TooLarge", ex.Width, ex.Height),
            };
        }
    }

    private void Apply(PreviewResult result)
    {
        if (result.Kind != PreviewKind.KeepAndBlock)
        {
            Preview = result.Bitmap;
            SourcePreview = result.Source;
            _frameBytes = result.FrameBytes;
            _frameSize = result.FrameSize;
            _packedExportAll = result.ExportAll;
            _packedFrameNumber = result.FrameNumber;
            OnPropertyChanged(nameof(Preview));
            OnPropertyChanged(nameof(SourcePreview));
        }

        Document = result.Document;
        if (result.Document is null)
        {
            CodeFiles.Clear();
            _codeIndex = -1;
            CodeIndex = 0;
            CodeText = result.Message ?? string.Empty;
            HighlightStart = 0;
            HighlightLength = 0;
        }
        else
        {
            FillCode(result.Document);
        }

        WarningText = string.Join(
            Environment.NewLine,
            result.Diagnostics.Select(item => UserText.Diagnostic(_loc, item)));
        OnPropertyChanged(nameof(Document));
        NotifyDerived();
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

    private bool ConfirmStep()
    {
        if (!_edits.HasEdits)
        {
            return true;
        }

        if (!_dialogs.Confirm(_loc.Get("Dialog.ResetEdits")))
        {
            return false;
        }

        _edits.Clear();
        _history.Clear();
        NotifyUndo();
        EditsChanged?.Invoke();
        return true;
    }

    private void CommitUi(bool preset)
    {
        if (_suppress)
        {
            return;
        }

        if (preset)
        {
            MarkPresetEdited();
        }

        UpdateAutomaticName();
        NotifyDerived();
        RefreshCommands();
        ParametersChanged?.Invoke();
        CommandsChanged?.Invoke();
        Schedule();
    }

    private void MarkPresetEdited()
    {
        if (_suppress)
        {
            return;
        }

        PresetBinding next = _binding.Edited();
        if (next == _binding)
        {
            return;
        }

        _binding = next;
        Presets.Show(next);
        OnPropertyChanged(nameof(PresetCaption));
    }

    private void UpdateAutomaticName()
    {
        if (_nameCustomized || !TryEffectiveSize(out int width, out int height))
        {
            return;
        }

        string name = BuildAutoName(width, height);
        if (name == _arrayName)
        {
            return;
        }

        _suppress = true;
        ArrayName = name;
        _suppress = false;
    }

    private bool IsAutomaticName(string name)
    {
        if (name == "image")
        {
            return true;
        }

        int width = _width;
        int height = _height;
        if (_sizeMode == TargetSizeMode.MatchSource && _image is not null)
        {
            (width, height) = Oriented(_image.Width, _image.Height, _rotation);
        }

        if (width < 1 || height < 1)
        {
            return false;
        }

        return name == BuildAutoName(width, height);
    }

    private string BuildAutoName(int width, int height)
    {
        int max = NameValidator.GetMaxLength(_format);
        return string.IsNullOrWhiteSpace(_sourcePath)
            ? DefaultNameBuilder.Build(null, width, height, ArrayNameKind.Image, max)
            : DefaultNameBuilder.FromFile(_sourcePath, width, height, ArrayNameKind.Image, max);
    }

    private bool TryEffectiveSize(out int width, out int height)
    {
        if (_sizeMode != TargetSizeMode.MatchSource)
        {
            width = _width;
            height = _height;
            return true;
        }

        if (_image is null)
        {
            width = _width;
            height = _height;
            return false;
        }

        (width, height) = Oriented(_image.Width, _image.Height, _rotation);
        return width <= ProcessingOptions.MaxTargetSide && height <= ProcessingOptions.MaxTargetSide;
    }

    private int EffectiveHeight() => TryEffectiveSize(out _, out int height) ? height : _height;

    private int FrameIndex => _image is null ? 0 : Math.Clamp(_selectedFrame, 1, _image.Frames.Count) - 1;

    private Preset? FindPreset(string name) => _catalog.Find(name) ?? _users.Find(name);

    private ProcessingOptions ToProcessing() => new()
    {
        Background = _background,
        Rotation = _rotation,
        FlipHorizontal = _flipHorizontal,
        FlipVertical = _flipVertical,
        SizeMode = _sizeMode,
        TargetWidth = _width,
        TargetHeight = _height,
        Fit = _fit,
        Alignment = _alignment,
        OffsetX = _offsetX,
        OffsetY = _offsetY,
        Resample = _resample,
        Binarize = _binarize,
        Threshold = _threshold,
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

    private void RebuildFrames()
    {
        Frames.Clear();
        int count = _image?.Frames.Count ?? 0;
        for (int frame = 1; frame <= count; frame++)
        {
            Frames.Add(frame);
        }

        OnPropertyChanged(nameof(HasImage));
        OnPropertyChanged(nameof(IsAnimated));
        OnPropertyChanged(nameof(SourceName));
    }

    private void NotifyDerived()
    {
        OnPropertyChanged(nameof(ManualSize));
        OnPropertyChanged(nameof(BitsEnabled));
        OnPropertyChanged(nameof(TraversalEnabled));
        OnPropertyChanged(nameof(AsmEnabled));
        OnPropertyChanged(nameof(Stm32Enabled));
        OnPropertyChanged(nameof(HasImage));
        OnPropertyChanged(nameof(IsAnimated));
        OnPropertyChanged(nameof(SourceName));
        OnPropertyChanged(nameof(ResultSizeText));
        OnPropertyChanged(nameof(PresetCaption));
        OnPropertyChanged(nameof(ShowCodeTabs));
    }

    private void NotifyUndo()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        CommandsChanged?.Invoke();
    }

    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(CanCopy));
        OnPropertyChanged(nameof(CanSaveOutput));
        CopyCommand.NotifyCanExecuteChanged();
        SaveOutputCommand.NotifyCanExecuteChanged();
        OpenImageCommand.NotifyCanExecuteChanged();
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

    private bool BeginChange<T>(ref T field, T value, bool step, bool preset, Action? beforeCommit = null, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        T previous = field;
        field = value;
        OnPropertyChanged(propertyName);
        beforeCommit?.Invoke();
        if (_suppress)
        {
            return false;
        }

        if (step && !ConfirmStep())
        {
            _suppress = true;
            field = previous;
            OnPropertyChanged(propertyName);
            beforeCommit?.Invoke();
            _suppress = false;
            return false;
        }

        CommitUi(preset);
        return true;
    }

    private void ApplySide(ref int stored, string text, bool width)
    {
        if (_suppress || _sizeMode != TargetSizeMode.Manual)
        {
            return;
        }

        if (!IntText.TryParse(text, ProcessingOptions.MinTargetSide, ProcessingOptions.MaxTargetSide, out int value))
        {
            RefreshCommands();
            Schedule();
            return;
        }

        if (value == stored)
        {
            return;
        }

        if (!ConfirmStep())
        {
            _suppress = true;
            if (width)
            {
                WidthText = IntText.Format(stored);
            }
            else
            {
                HeightText = IntText.Format(stored);
            }

            _suppress = false;
            return;
        }

        stored = value;
        CommitUi(preset: true);
    }

    private void ApplyOffset(ref int stored, string text, bool horizontal)
    {
        if (_suppress)
        {
            return;
        }

        if (!IntText.TryParse(text, ProcessingOptions.MinOffset, ProcessingOptions.MaxOffset, out int value))
        {
            RefreshCommands();
            Schedule();
            return;
        }

        if (value == stored)
        {
            return;
        }

        if (!ConfirmStep())
        {
            _suppress = true;
            if (horizontal)
            {
                OffsetXText = IntText.Format(stored);
            }
            else
            {
                OffsetYText = IntText.Format(stored);
            }

            _suppress = false;
            return;
        }

        stored = value;
        CommitUi(preset: false);
    }

    private static FramePixelOverrides CloneEdits(FramePixelOverrides source)
    {
        var copy = new FramePixelOverrides();
        foreach (int frame in source.EditedFrames())
        {
            PixelOverrides target = copy.ForFrame(frame);
            if (!source.TryGetFrame(frame, out PixelOverrides? pixels) || pixels is null)
            {
                continue;
            }

            foreach ((int x, int y, bool active) in pixels.Entries())
            {
                target.Set(x, y, active);
            }
        }

        return copy;
    }

    private static (int Width, int Height) Oriented(int width, int height, Rotation rotation) =>
        rotation is Rotation.Rotate90 or Rotation.Rotate270 ? (height, width) : (width, height);

    public static ValidationResult? ValidateWidthText(string? text, ValidationContext context)
    {
        var vm = (ImageConverterViewModel)context.ObjectInstance!;
        if (vm._sizeMode != TargetSizeMode.Manual)
        {
            return null;
        }

        return IntText.TryParse(text, ProcessingOptions.MinTargetSide, ProcessingOptions.MaxTargetSide, out _)
            ? null
            : new ValidationResult(vm._loc.Get("Error.Width"));
    }

    public static ValidationResult? ValidateHeightText(string? text, ValidationContext context)
    {
        var vm = (ImageConverterViewModel)context.ObjectInstance!;
        if (vm._sizeMode != TargetSizeMode.Manual)
        {
            return null;
        }

        return IntText.TryParse(text, ProcessingOptions.MinTargetSide, ProcessingOptions.MaxTargetSide, out _)
            ? null
            : new ValidationResult(vm._loc.Get("Error.Height"));
    }

    public static ValidationResult? ValidateOffset(string? text, ValidationContext context)
    {
        var vm = (ImageConverterViewModel)context.ObjectInstance!;
        return IntText.TryParse(text, ProcessingOptions.MinOffset, ProcessingOptions.MaxOffset, out _)
            ? null
            : new ValidationResult(vm._loc.Get("Error.Offset"));
    }

    public static ValidationResult? ValidateThreshold(string? text, ValidationContext context)
    {
        var vm = (ImageConverterViewModel)context.ObjectInstance!;
        return IntText.TryParse(text, 0, 255, out _)
            ? null
            : new ValidationResult(vm._loc.Get("Error.Threshold"));
    }

    public static ValidationResult? ValidateBytes(string? text, ValidationContext context)
    {
        var vm = (ImageConverterViewModel)context.ObjectInstance!;
        return IntText.TryParse(text, OutputOptions.MinBytesPerLine, OutputOptions.MaxBytesPerLine, out _)
            ? null
            : new ValidationResult(vm._loc.Get("Error.BytesPerLine"));
    }

    public static ValidationResult? ValidateArrayName(string? name, ValidationContext context)
    {
        var vm = (ImageConverterViewModel)context.ObjectInstance!;
        NameValidationResult result = NameValidator.Validate(name, vm._format);
        return result.IsValid ? null : new ValidationResult(UserText.Name(vm._loc, result));
    }
}
