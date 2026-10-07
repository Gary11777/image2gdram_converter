using System.Windows.Controls;
using image2gdram_converter.ViewModels;

namespace image2gdram_converter.Views;

public partial class FontGeneratorView : UserControl
{
    public FontGeneratorView()
    {
        InitializeComponent();
        EditorGrid.StrokeCompleted += (_, args) =>
        {
            if (DataContext is FontGeneratorViewModel model)
            {
                model.Editor.ApplyStroke(args.Points, args.Paint);
            }
        };
        EditorGrid.HoverChanged += (_, args) =>
        {
            if (DataContext is FontGeneratorViewModel model)
            {
                model.SetHover(args.X, args.Y);
            }
        };
    }
}
