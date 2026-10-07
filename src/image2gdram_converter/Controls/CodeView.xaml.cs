using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using image2gdram_converter.ViewModels;

namespace image2gdram_converter.Controls;

/// <summary>Только чтение: текст файла и подсветка байта по карте генератора (решение N-17).</summary>
public partial class CodeView : UserControl
{
    private readonly TextEditor _editor;
    private readonly ByteHighlighter _highlighter = new();
    private ICodeSurface? _model;

    public CodeView()
    {
        InitializeComponent();
        _editor = new TextEditor
        {
            IsReadOnly = true,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            ShowLineNumbers = true,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        _editor.Options.EnableHyperlinks = false;
        _editor.Options.EnableEmailHyperlinks = false;
        _editor.TextArea.TextView.BackgroundRenderers.Add(_highlighter);
        EditorHost.Child = _editor;
        DataContextChanged += (_, _) => Attach(DataContext as ICodeSurface);
    }

    private void Attach(ICodeSurface? model)
    {
        if (_model is not null)
        {
            _model.PropertyChanged -= OnModelChanged;
        }

        _model = model;
        if (_model is null)
        {
            return;
        }

        _model.PropertyChanged += OnModelChanged;
        Refresh(scroll: false);
    }

    private void OnModelChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(ICodeSurface.CodeText)
            or nameof(ICodeSurface.HighlightStart)
            or nameof(ICodeSurface.HighlightLength)
            or "")
        {
            Refresh(scroll: args.PropertyName != nameof(ICodeSurface.CodeText) || _model?.HighlightLength > 0);
        }
    }

    private void Refresh(bool scroll)
    {
        if (_model is null)
        {
            return;
        }

        if (_editor.Text != _model.CodeText)
        {
            _editor.Text = _model.CodeText;
        }

        _highlighter.Start = _model.HighlightStart;
        _highlighter.Length = _model.HighlightLength;
        _editor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
        if (!scroll || _highlighter.Length <= 0 || _editor.Document is null)
        {
            return;
        }

        int start = Math.Clamp(_highlighter.Start, 0, Math.Max(0, _editor.Document.TextLength - 1));
        DocumentLine line = _editor.Document.GetLineByOffset(start);
        _editor.ScrollTo(line.LineNumber, 0);
    }

    private sealed class ByteHighlighter : IBackgroundRenderer
    {
        private static readonly Brush Fill = Create();

        public int Start { get; set; }

        public int Length { get; set; }

        public KnownLayer Layer => KnownLayer.Selection;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (Length <= 0 || textView.Document is null || Start < 0 || Start >= textView.Document.TextLength)
            {
                return;
            }

            int length = Math.Min(Length, textView.Document.TextLength - Start);
            var segment = new TextSegment { StartOffset = Start, Length = length };
            foreach (Rect rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                drawingContext.DrawRectangle(Fill, null, rect);
            }
        }

        private static Brush Create()
        {
            var brush = new SolidColorBrush(Color.FromArgb(120, 255, 214, 102));
            brush.Freeze();
            return brush;
        }
    }
}
