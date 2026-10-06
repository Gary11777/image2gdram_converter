using System.Windows.Controls;
using image2gdram_converter.Controls;
using image2gdram_converter.ViewModels;

namespace image2gdram_converter.Views;

public partial class ImageConverterView : UserControl
{
    public ImageConverterView()
    {
        InitializeComponent();
        Pixels.StrokeCompleted += (_, args) =>
        {
            if (DataContext is ImageConverterViewModel model)
            {
                model.ApplyStroke(args.Points, args.Paint);
            }
        };
        Pixels.HoverChanged += (_, args) =>
        {
            if (DataContext is ImageConverterViewModel model)
            {
                model.SetHover(args.X, args.Y);
            }
        };
    }
}
