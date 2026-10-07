using System.ComponentModel.DataAnnotations;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;

namespace image2gdram_converter.ViewModels;

public sealed partial class ImageConverterViewModel
{
    public BackgroundColor Background
    {
        get => _background;
        set => BeginChange(ref _background, value, step: true, preset: false);
    }

    public Rotation Rotation
    {
        get => _rotation;
        set => BeginChange(ref _rotation, value, step: true, preset: false);
    }

    public bool FlipHorizontal
    {
        get => _flipHorizontal;
        set => BeginChange(ref _flipHorizontal, value, step: true, preset: false);
    }

    public bool FlipVertical
    {
        get => _flipVertical;
        set => BeginChange(ref _flipVertical, value, step: true, preset: false);
    }

    public TargetSizeMode SizeMode
    {
        get => _sizeMode;
        set => BeginChange(ref _sizeMode, value, step: true, preset: true, () =>
        {
            ValidateProperty(_widthText, nameof(WidthText));
            ValidateProperty(_heightText, nameof(HeightText));
        });
    }

    [CustomValidation(typeof(ImageConverterViewModel), nameof(ValidateWidthText))]
    public string WidthText
    {
        get => _widthText;
        set
        {
            if (!SetProperty(ref _widthText, value, true))
            {
                return;
            }

            ApplySide(ref _width, value, width: true);
        }
    }

    [CustomValidation(typeof(ImageConverterViewModel), nameof(ValidateHeightText))]
    public string HeightText
    {
        get => _heightText;
        set
        {
            if (!SetProperty(ref _heightText, value, true))
            {
                return;
            }

            ApplySide(ref _height, value, width: false);
        }
    }

    public FitMode Fit
    {
        get => _fit;
        set => BeginChange(ref _fit, value, step: true, preset: false);
    }

    public Alignment Alignment
    {
        get => _alignment;
        set => BeginChange(ref _alignment, value, step: true, preset: false);
    }

    [CustomValidation(typeof(ImageConverterViewModel), nameof(ValidateOffset))]
    public string OffsetXText
    {
        get => _offsetXText;
        set
        {
            if (!SetProperty(ref _offsetXText, value, true))
            {
                return;
            }

            ApplyOffset(ref _offsetX, value, horizontal: true);
        }
    }

    [CustomValidation(typeof(ImageConverterViewModel), nameof(ValidateOffset))]
    public string OffsetYText
    {
        get => _offsetYText;
        set
        {
            if (!SetProperty(ref _offsetYText, value, true))
            {
                return;
            }

            ApplyOffset(ref _offsetY, value, horizontal: false);
        }
    }

    public ResampleMode Resample
    {
        get => _resample;
        set => BeginChange(ref _resample, value, step: true, preset: false);
    }

    public BinarizeMode Binarize
    {
        get => _binarize;
        set => BeginChange(ref _binarize, value, step: true, preset: false);
    }

    public int Threshold
    {
        get => _threshold;
        set
        {
            int previous = _threshold;
            if (!SetProperty(ref _threshold, value))
            {
                return;
            }

            bool outer = _suppress;
            string text = IntText.Format(value);
            if (_thresholdText != text)
            {
                _suppress = true;
                ThresholdText = text;
                _suppress = outer;
            }

            if (outer)
            {
                return;
            }

            if (!ConfirmStep())
            {
                _suppress = true;
                Threshold = previous;
                _suppress = false;
                return;
            }

            CommitUi(preset: false);
        }
    }

    [CustomValidation(typeof(ImageConverterViewModel), nameof(ValidateThreshold))]
    public string ThresholdText
    {
        get => _thresholdText;
        set
        {
            if (!SetProperty(ref _thresholdText, value, true))
            {
                return;
            }

            if (_suppress)
            {
                return;
            }

            if (!IntText.TryParse(value, 0, 255, out int parsed))
            {
                RefreshCommands();
                Schedule();
                return;
            }

            if (parsed != _threshold)
            {
                Threshold = parsed;
            }
            else
            {
                Unblock();
            }
        }
    }

    public PackDirection Direction
    {
        get => _direction;
        set => BeginChange(ref _direction, value, step: false, preset: true);
    }

    public BitOrder BitOrder
    {
        get => _bitOrder;
        set => BeginChange(ref _bitOrder, value, step: false, preset: true);
    }

    public int BitsPerByte
    {
        get => _bits;
        set
        {
            if (value is 6 or 8)
            {
                BeginChange(ref _bits, value, step: false, preset: true);
            }
        }
    }

    public PageTraversal Traversal
    {
        get => _traversal;
        set => BeginChange(ref _traversal, value, step: false, preset: true);
    }

    public bool Invert
    {
        get => _invert;
        set => BeginChange(ref _invert, value, step: false, preset: true);
    }

    public ColorScheme Scheme
    {
        get => _scheme;
        set => BeginChange(ref _scheme, value, step: false, preset: true);
    }

    public OutputFormat Format
    {
        get => _format;
        set => BeginChange(ref _format, value, step: false, preset: false, () => ValidateProperty(_arrayName, nameof(ArrayName)));
    }

    [CustomValidation(typeof(ImageConverterViewModel), nameof(ValidateArrayName))]
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

            CommitUi(preset: false);
        }
    }

    public OutputEncoding Encoding
    {
        get => _encoding;
        set => BeginChange(ref _encoding, value, step: false, preset: false);
    }

    [CustomValidation(typeof(ImageConverterViewModel), nameof(ValidateBytes))]
    public string BytesPerLineText
    {
        get => _bytesText;
        set
        {
            if (!SetProperty(ref _bytesText, value, true))
            {
                return;
            }

            if (_suppress)
            {
                return;
            }

            if (!IntText.TryParse(value, OutputOptions.MinBytesPerLine, OutputOptions.MaxBytesPerLine, out int parsed))
            {
                RefreshCommands();
                Schedule();
                return;
            }

            if (parsed == _bytesPerLine)
            {
                Unblock();
                return;
            }

            _bytesPerLine = parsed;
            CommitUi(preset: false);
        }
    }

    public AsmNumberFormat AsmNumbers
    {
        get => _asmNumbers;
        set => BeginChange(ref _asmNumbers, value, step: false, preset: false);
    }

    public AsmFileExtension AsmExtension
    {
        get => _asmExtension;
        set => BeginChange(ref _asmExtension, value, step: false, preset: false);
    }

    public Stm32ElementType Stm32Type
    {
        get => _stm32;
        set => BeginChange(ref _stm32, value, step: false, preset: false);
    }

    public bool IncludeDate
    {
        get => _includeDate;
        set => BeginChange(ref _includeDate, value, step: false, preset: false);
    }

    public int SelectedFrame
    {
        get => _selectedFrame;
        set
        {
            int max = Math.Max(1, _image?.Frames.Count ?? 1);
            BeginChange(ref _selectedFrame, Math.Clamp(value, 1, max), step: false, preset: false);
        }
    }

    public bool ExportAllFrames
    {
        get => _exportAll;
        set => BeginChange(ref _exportAll, value, step: false, preset: false);
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
}
