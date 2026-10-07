using System.ComponentModel.DataAnnotations;
using System.IO;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Text;

namespace image2gdram_converter.ViewModels;

public sealed partial class FontGeneratorViewModel
{
    public string PresetCaption => Presets.Caption;

    public bool IsTrueType => _sourceKind == FontSourceKind.TrueType;

    public bool IsSheet => _sourceKind == FontSourceKind.Sheet;

    public bool IsImport => _sourceKind == FontSourceKind.Import;

    public bool ShowRanges => _sourceKind is FontSourceKind.TrueType or FontSourceKind.Sheet;

    public bool ShowGlyphThreshold => IsTrueType && _renderMode == GlyphRenderMode.Antialiased;

    public bool BitsEnabled => _direction == PackDirection.Horizontal;

    public bool TraversalEnabled => _direction == PackDirection.Vertical && _cell.Height > 8;

    public bool AsmEnabled => _format is OutputFormat.A51Module or OutputFormat.A51Include;

    public bool Stm32Enabled => _format == OutputFormat.CStm32;

    public bool ShowCodeTabs => CodeFiles.Count > 1;

    public bool HasMissing => _missing.Count > 0;

    public string MissingText =>
        _missing.Count == 0 ? string.Empty : string.Join(Environment.NewLine, _missing.Select(FormatCode));

    public string SizeLine
    {
        get
        {
            int per = _fontBytes is { Length: > 0 }
                ? _fontBytes.Length / FontTable.CharCount
                : FontTable.GetBytesPerChar(_cell, _packers.Get(ToPacking()), ToPacking());
            return _loc.Format(
                "Font.SizeLine",
                _cell.ToString(),
                RussianPlural.Bytes(per),
                RussianPlural.Bytes(per * FontTable.CharCount));
        }
    }

    public string SheetCaption => string.IsNullOrWhiteSpace(_sheetPath) ? string.Empty : Path.GetFileName(_sheetPath);

    public string ImportCaption
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_importPath))
            {
                return string.Empty;
            }

            string file = Path.GetFileName(_importPath);
            return string.IsNullOrWhiteSpace(_importArrayName) ? file : file + " (" + _importArrayName + ")";
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

    public FontSourceKind SourceKind
    {
        get => _sourceKind;
        set => BeginChange(ref _sourceKind, value, source: true, preset: false, kindChanged: true, () =>
        {
            ValidateAllProperties();
            NotifyDerived();
        });
    }

    public string Family
    {
        get => _family;
        set
        {
            string next = value ?? string.Empty;
            if (!BeginChange(ref _family, next, source: true, preset: false))
            {
                return;
            }
        }
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string FontSizeText
    {
        get => _fontSizeText;
        set => SetNumber(ref _fontSizeText, value, ref _fontSize, FontFaceSpec.MinSizePx, FontFaceSpec.MaxSizePx, source: true, preset: false);
    }

    public bool Bold
    {
        get => _bold;
        set => BeginChange(ref _bold, value, source: true, preset: false);
    }

    public bool Italic
    {
        get => _italic;
        set => BeginChange(ref _italic, value, source: true, preset: false);
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string OffsetXText
    {
        get => _offsetXText;
        set => SetNumber(ref _offsetXText, value, ref _offsetX, TrueTypeOptions.MinOffset, TrueTypeOptions.MaxOffset, source: true, preset: false);
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string OffsetYText
    {
        get => _offsetYText;
        set => SetNumber(ref _offsetYText, value, ref _offsetY, TrueTypeOptions.MinOffset, TrueTypeOptions.MaxOffset, source: true, preset: false);
    }

    public GlyphRenderMode RenderMode
    {
        get => _renderMode;
        set => BeginChange(ref _renderMode, value, source: true, preset: false, before: NotifyDerived);
    }

    public int GlyphThreshold
    {
        get => _glyphThreshold;
        set
        {
            if (!SetProperty(ref _glyphThreshold, value))
            {
                return;
            }

            SyncText(ref _glyphThresholdText, value, nameof(GlyphThresholdText));
            if (!_suppress)
            {
                Commit(preset: false, source: true);
            }
        }
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string GlyphThresholdText
    {
        get => _glyphThresholdText;
        set
        {
            if (!SetProperty(ref _glyphThresholdText, value, true))
            {
                return;
            }

            if (_suppress || !IntText.TryParse(value, 0, 255, out int parsed))
            {
                if (!_suppress)
                {
                    RefreshCommands();
                    Schedule();
                }

                return;
            }

            if (parsed != _glyphThreshold)
            {
                GlyphThreshold = parsed;
            }
        }
    }

    public FontCellSize Cell
    {
        get => _cell;
        set
        {
            if (value is null || value == _cell)
            {
                return;
            }

            if (_suppress)
            {
                _cell = value;
                OnPropertyChanged();
                return;
            }

            if (_table.HasManualEdits && !_dialogs.Confirm(_loc.Get("Dialog.ReplaceFont")))
            {
                OnPropertyChanged();
                return;
            }

            FontCellSize previous = _cell;
            FollowSheetCell(previous, value);
            _cell = value;
            _table = new FontTable(_cell);
            _history.Clear();
            NotifyUndo();
            EditsChanged?.Invoke();
            OnPropertyChanged();
            TouchTable();
            Commit(preset: false, source: true);
        }
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string SheetWidthText
    {
        get => _sheetWidthText;
        set => SetNumber(ref _sheetWidthText, value, ref _sheetWidth, 1, 64, source: true, preset: false);
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string SheetHeightText
    {
        get => _sheetHeightText;
        set => SetNumber(ref _sheetHeightText, value, ref _sheetHeight, 1, 64, source: true, preset: false);
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string MarginXText
    {
        get => _marginXText;
        set => SetNumber(ref _marginXText, value, ref _marginX, 0, 1024, source: true, preset: false);
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string MarginYText
    {
        get => _marginYText;
        set => SetNumber(ref _marginYText, value, ref _marginY, 0, 1024, source: true, preset: false);
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string SpacingXText
    {
        get => _spacingXText;
        set => SetNumber(ref _spacingXText, value, ref _spacingX, 0, 256, source: true, preset: false);
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string SpacingYText
    {
        get => _spacingYText;
        set => SetNumber(ref _spacingYText, value, ref _spacingY, 0, 256, source: true, preset: false);
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string CharsPerRowText
    {
        get => _charsPerRowText;
        set => SetNumber(ref _charsPerRowText, value, ref _charsPerRow, 1, 256, source: true, preset: false);
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string FirstCodeText
    {
        get => _firstCodeText;
        set => SetNumber(ref _firstCodeText, value, ref _firstCode, 0, 255, source: true, preset: false);
    }

    public int SheetThreshold
    {
        get => _sheetThreshold;
        set
        {
            if (!SetProperty(ref _sheetThreshold, value))
            {
                return;
            }

            SyncText(ref _sheetThresholdText, value, nameof(SheetThresholdText));
            if (!_suppress)
            {
                Commit(preset: false, source: true);
            }
        }
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string SheetThresholdText
    {
        get => _sheetThresholdText;
        set
        {
            if (!SetProperty(ref _sheetThresholdText, value, true))
            {
                return;
            }

            if (_suppress || !IntText.TryParse(value, 0, 255, out int parsed))
            {
                if (!_suppress)
                {
                    RefreshCommands();
                    Schedule();
                }

                return;
            }

            if (parsed != _sheetThreshold)
            {
                SheetThreshold = parsed;
            }
        }
    }

    public bool Latin
    {
        get => _ranges.HasFlag(CharRangePreset.Latin);
        set => SetFlag(CharRangePreset.Latin, value);
    }

    public bool Cyrillic
    {
        get => _ranges.HasFlag(CharRangePreset.Cyrillic);
        set => SetFlag(CharRangePreset.Cyrillic, value);
    }

    public bool OtherRange
    {
        get => _ranges.HasFlag(CharRangePreset.OtherCp1251);
        set => SetFlag(CharRangePreset.OtherCp1251, value);
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateRange))]
    public string CustomRangeText
    {
        get => _customRangeText;
        set
        {
            if (!SetProperty(ref _customRangeText, value ?? string.Empty, true))
            {
                return;
            }

            if (_suppress)
            {
                return;
            }

            if (!CharRangeSet.TryParseCustom(_customRangeText, out IReadOnlyList<int> codes, out _))
            {
                RefreshCommands();
                Schedule();
                return;
            }

            _customCodes = codes.ToArray();
            Commit(preset: false, source: true);
        }
    }

    public string PreviewText
    {
        get => _previewText;
        set
        {
            if (!SetProperty(ref _previewText, value ?? string.Empty))
            {
                return;
            }

            if (!_suppress)
            {
                ParametersChanged?.Invoke();
            }
        }
    }

    public int PreviewScale
    {
        get => _previewScale;
        set
        {
            int scale = Math.Clamp(value, 1, 8);
            if (!SetProperty(ref _previewScale, scale))
            {
                return;
            }

            if (!_suppress)
            {
                ParametersChanged?.Invoke();
            }
        }
    }

    public PackDirection Direction
    {
        get => _direction;
        set => BeginChange(ref _direction, value, source: false, preset: true, before: NotifyDerived);
    }

    public BitOrder BitOrder
    {
        get => _bitOrder;
        set => BeginChange(ref _bitOrder, value, source: false, preset: true);
    }

    public int BitsPerByte
    {
        get => _bits;
        set
        {
            if (value is 6 or 8)
            {
                BeginChange(ref _bits, value, source: false, preset: true);
            }
        }
    }

    public PageTraversal Traversal
    {
        get => _traversal;
        set => BeginChange(ref _traversal, value, source: false, preset: true);
    }

    public bool Invert
    {
        get => _invert;
        set => BeginChange(ref _invert, value, source: false, preset: true);
    }

    public ColorScheme Scheme
    {
        get => _scheme;
        set => BeginChange(ref _scheme, value, source: false, preset: true);
    }

    public OutputFormat Format
    {
        get => _format;
        set => BeginChange(ref _format, value, source: false, preset: false, before: () =>
        {
            ValidateProperty(_arrayName, nameof(ArrayName));
            NotifyDerived();
        });
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateArrayName))]
    public string ArrayName
    {
        get => _arrayName;
        set
        {
            if (!SetProperty(ref _arrayName, value, true))
            {
                return;
            }

            if (_suppress)
            {
                return;
            }

            _nameCustomized = true;
            RefreshCommands();
            if (FirstError() is not null)
            {
                Schedule();
                return;
            }

            Commit(preset: false, source: false);
        }
    }

    public OutputEncoding Encoding
    {
        get => _encoding;
        set => BeginChange(ref _encoding, value, source: false, preset: false);
    }

    [CustomValidation(typeof(FontGeneratorViewModel), nameof(ValidateNumber))]
    public string BytesPerLineText
    {
        get => _bytesText;
        set => SetNumber(ref _bytesText, value, ref _bytesPerLine, OutputOptions.MinBytesPerLine, OutputOptions.MaxBytesPerLine, source: false, preset: false);
    }

    public AsmNumberFormat AsmNumbers
    {
        get => _asmNumbers;
        set => BeginChange(ref _asmNumbers, value, source: false, preset: false);
    }

    public AsmFileExtension AsmExtension
    {
        get => _asmExtension;
        set => BeginChange(ref _asmExtension, value, source: false, preset: false);
    }

    public Stm32ElementType Stm32Type
    {
        get => _stm32;
        set => BeginChange(ref _stm32, value, source: false, preset: false);
    }

    public bool IncludeDate
    {
        get => _includeDate;
        set => BeginChange(ref _includeDate, value, source: false, preset: false);
    }

    public bool ShowGrid
    {
        get => _showGrid;
        set => SetProperty(ref _showGrid, value);
    }

    public bool FitToWindow
    {
        get => _fitWindow;
        set => SetProperty(ref _fitWindow, value);
    }

    public int Scale
    {
        get => _scale;
        set => SetProperty(ref _scale, value);
    }

    public ZoomChoice? Zoom
    {
        get => _zoom;
        set
        {
            if (!SetProperty(ref _zoom, value) || value is null || _suppress)
            {
                return;
            }

            FitToWindow = value.Scale is null;
            if (value.Scale is int scale)
            {
                Scale = scale;
            }
        }
    }

    public static ValidationResult? ValidateNumber(string? text, ValidationContext context)
    {
        var vm = (FontGeneratorViewModel)context.ObjectInstance!;
        if (!vm.NumberApplies(context.MemberName))
        {
            return null;
        }

        (int min, int max, string key) = context.MemberName switch
        {
            nameof(FontSizeText) => (FontFaceSpec.MinSizePx, FontFaceSpec.MaxSizePx, "Error.FontSize"),
            nameof(OffsetXText) or nameof(OffsetYText) => (TrueTypeOptions.MinOffset, TrueTypeOptions.MaxOffset, "Error.GlyphOffset"),
            nameof(SheetWidthText) or nameof(SheetHeightText) => (1, 64, "Error.SheetCell"),
            nameof(MarginXText) or nameof(MarginYText) => (0, 1024, "Error.Margin"),
            nameof(SpacingXText) or nameof(SpacingYText) => (0, 256, "Error.Spacing"),
            nameof(CharsPerRowText) => (1, 256, "Error.CharsPerRow"),
            nameof(FirstCodeText) => (0, 255, "Error.FirstCode"),
            nameof(BytesPerLineText) => (OutputOptions.MinBytesPerLine, OutputOptions.MaxBytesPerLine, "Error.BytesPerLine"),
            _ => (0, 255, "Error.Threshold"),
        };
        return IntText.TryParse(text, min, max, out _) ? null : new ValidationResult(vm._loc.Get(key));
    }

    public static ValidationResult? ValidateRange(string? text, ValidationContext context)
    {
        var vm = (FontGeneratorViewModel)context.ObjectInstance!;
        if (CharRangeSet.TryParseCustom(text ?? string.Empty, out _, out CharRangeParseError? error) || error is null)
        {
            return null;
        }

        string key = error.Kind switch
        {
            CharRangeParseErrorKind.OutOfRange => "Error.Range.Bounds",
            CharRangeParseErrorKind.ReversedRange => "Error.Range.Order",
            CharRangeParseErrorKind.MisplacedDash => "Error.Range.Dash",
            _ => "Error.Range.Number",
        };
        return new ValidationResult(vm._loc.Format(key, error.Position + 1, error.Token));
    }

    public static ValidationResult? ValidateArrayName(string? name, ValidationContext context)
    {
        var vm = (FontGeneratorViewModel)context.ObjectInstance!;
        NameValidationResult result = NameValidator.Validate(name, vm._format);
        return result.IsValid ? null : new ValidationResult(UserText.Name(vm._loc, result));
    }

    private void SyncText(ref string field, int value, string propertyName)
    {
        string text = IntText.Format(value);
        if (field == text)
        {
            return;
        }

        bool outer = _suppress;
        _suppress = true;
        field = text;
        OnPropertyChanged(propertyName);
        ValidateProperty(field, propertyName);
        _suppress = outer;
    }
}
