using System.ComponentModel;
using System.Windows;
using image2gdram_converter.ViewModels;

namespace image2gdram_converter;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, CancelEventArgs args)
    {
        if (DataContext is MainViewModel model && !model.TryClose())
        {
            args.Cancel = true;
        }
    }
}
