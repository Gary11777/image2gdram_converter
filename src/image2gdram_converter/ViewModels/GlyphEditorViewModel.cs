using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Packing;
using image2gdram_converter.Services;

namespace image2gdram_converter.ViewModels;

/// <summary>
/// Редактор одного символа: переключение пикселей, очистка, инверсия, сдвиг, копирование и вставка
/// (п. 5.4 ТЗ, решения D-14, N-22).
/// </summary>
public sealed partial class GlyphEditorViewModel : ObservableObject
{
    private readonly IClipboardService _clipboard;
    private readonly Func<bool> _invert;
    private readonly Func<int, MonoBitmap> _current;
    private readonly Action<int, MonoBitmap?> _apply;
    private readonly Func<int, string> _captionOf;
    private int _selectedCode;

    public GlyphEditorViewModel(
        IClipboardService clipboard,
        Func<bool> invert,
        Func<int, MonoBitmap> current,
        Action<int, MonoBitmap?> apply,
        Func<int, string> captionOf)
    {
        ArgumentNullException.ThrowIfNull(clipboard);
        ArgumentNullException.ThrowIfNull(invert);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(captionOf);
        _clipboard = clipboard;
        _invert = invert;
        _current = current;
        _apply = apply;
        _captionOf = captionOf;
        Reload();
    }

    public MonoBitmap? Glyph { get; private set; }

    public string Caption { get; private set; } = string.Empty;

    public int SelectedCode
    {
        get => _selectedCode;
        set
        {
            int code = Math.Clamp(value, 0, FontTable.CharCount - 1);
            if (!SetProperty(ref _selectedCode, code))
            {
                return;
            }

            Reload();
        }
    }

    public void Reload()
    {
        Glyph = _current(SelectedCode);
        Caption = _captionOf(SelectedCode);
        OnPropertyChanged(nameof(Glyph));
        OnPropertyChanged(nameof(Caption));
    }

    public void ApplyStroke(IReadOnlyList<(int X, int Y)> points, StrokePaint paint)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            return;
        }

        MonoBitmap next = _current(SelectedCode).Clone();
        if (paint == StrokePaint.Toggle)
        {
            (int x, int y) = points[0];
            if (Inside(next, x, y))
            {
                next[x, y] = !next[x, y];
            }
        }
        else
        {
            bool active = PixelPaint.Logical(_invert(), paint);
            foreach ((int x, int y) in points)
            {
                if (Inside(next, x, y))
                {
                    next[x, y] = active;
                }
            }
        }

        _apply(SelectedCode, next);
    }

    [RelayCommand]
    public void ClearGlyph() => _apply(SelectedCode, GlyphOps.Clear(_current(SelectedCode)));

    [RelayCommand]
    public void InvertGlyph() => _apply(SelectedCode, GlyphOps.Invert(_current(SelectedCode)));

    [RelayCommand]
    public void ShiftLeft() => Shift(ShiftDirection.Left);

    [RelayCommand]
    public void ShiftRight() => Shift(ShiftDirection.Right);

    [RelayCommand]
    public void ShiftUp() => Shift(ShiftDirection.Up);

    [RelayCommand]
    public void ShiftDown() => Shift(ShiftDirection.Down);

    [RelayCommand]
    public void CopyGlyph() => _clipboard.SetGlyph(_current(SelectedCode), _invert());

    [RelayCommand]
    public void PasteGlyph()
    {
        if (!_clipboard.TryGetGlyph(_invert(), out MonoBitmap? pasted) || pasted is null)
        {
            return;
        }

        MonoBitmap current = _current(SelectedCode);
        FontCellSize cell = FontCellSize.Get(current.Width, current.Height);
        _apply(SelectedCode, GlyphOps.Fit(pasted, cell));
    }

    [RelayCommand]
    public void RevertGlyph() => _apply(SelectedCode, null);

    private void Shift(ShiftDirection direction) =>
        _apply(SelectedCode, GlyphOps.Shift(_current(SelectedCode), direction));

    private static bool Inside(MonoBitmap bitmap, int x, int y) =>
        (uint)x < (uint)bitmap.Width && (uint)y < (uint)bitmap.Height;
}
