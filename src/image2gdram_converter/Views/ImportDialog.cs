using System.Windows;
using System.Windows.Controls;
using image2gdram_converter;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Fonts.Import;
using Image2Gdram.Core.Packing;
using image2gdram_converter.Services;

namespace image2gdram_converter.Views;

/// <summary>?????? ???????? ????? ? ?????????, ???????? ????? ?????????????? ?? ????????.</summary>
public sealed class ImportDialog : Window
{
    private readonly ListBox _list;
    private readonly TextBlock _error;
    private readonly ComboBox _cell;
    private readonly ComboBox _direction;
    private readonly ComboBox _bitOrder;
    private readonly ComboBox _bits;
    private readonly ComboBox _traversal;
    private readonly CheckBox _invert;
    private readonly Button _ok;

    public ImportDialog(
        ILocalizationService text,
        IReadOnlyList<ImportedArray> arrays,
        FontCellSize cell,
        PackingOptions packing)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(arrays);
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(packing);
        Title = text.Get("Import.Title");
        Width = 460;
        SizeToContent = SizeToContent.Height;
        MaxHeight = 640;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Window? owner = Application.Current?.MainWindow;
        if (owner is { IsVisible: true })
        {
            Owner = owner;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        var rows = arrays.Select(array => new Row(Caption(text, array), array.Error is null ? null : UserText.ImportIssue(text, array.Error))).ToList();
        _list = new ListBox { ItemsSource = rows, DisplayMemberPath = nameof(Row.Caption), Height = 160 };
        _error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };
        _cell = Combo(OptionLists.Cells(text), cell);
        _direction = Combo(OptionLists.Directions(text), packing.Direction);
        _bitOrder = Combo(OptionLists.BitOrders(text), packing.BitOrder);
        _bits = Combo(OptionLists.BitCounts(text), packing.BitsPerByte);
        _traversal = Combo(OptionLists.Traversals(text), packing.PageTraversal);
        _invert = new CheckBox { Content = text.Get("Check.Invert"), IsChecked = packing.Invert, Margin = new Thickness(0, 6, 0, 0) };
        _ok = new Button { Content = text.Get("Dialog.Ok"), MinWidth = 100, Margin = new Thickness(8, 0, 0, 0), IsDefault = true };
        var cancel = new Button { Content = text.Get("Dialog.Cancel"), MinWidth = 100, Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
        _ok.Click += (_, _) => Accept();
        _list.SelectionChanged += (_, _) => UpdateSelection();

        var form = new StackPanel { Margin = new Thickness(16) };
        form.Children.Add(new TextBlock { Text = text.Get("Import.Hint"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
        form.Children.Add(_list);
        form.Children.Add(_error);
        form.Children.Add(Field(text.Get("Label.Cell"), _cell));
        form.Children.Add(Field(text.Get("Label.Direction"), _direction));
        form.Children.Add(Field(text.Get("Label.BitOrder"), _bitOrder));
        form.Children.Add(Field(text.Get("Label.Bits"), _bits));
        form.Children.Add(Field(text.Get("Label.Traversal"), _traversal));
        form.Children.Add(_invert);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0),
        };
        buttons.Children.Add(_ok);
        buttons.Children.Add(cancel);
        form.Children.Add(buttons);
        Content = new ScrollViewer { Content = form, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        if (rows.Count > 0)
        {
            _list.SelectedIndex = 0;
        }

        UpdateSelection();
    }

    public ImportPick? Pick { get; private set; }

    private void Accept()
    {
        if (_list.SelectedItem is not Row row || row.Error is not null)
        {
            return;
        }

        if (_cell.SelectedValue is not FontCellSize cell
            || _direction.SelectedValue is not PackDirection direction
            || _bitOrder.SelectedValue is not BitOrder bitOrder
            || _bits.SelectedValue is not int bits
            || _traversal.SelectedValue is not PageTraversal traversal)
        {
            return;
        }

        Pick = new ImportPick(
            _list.SelectedIndex,
            cell,
            new PackingOptions
            {
                Direction = direction,
                BitOrder = bitOrder,
                BitsPerByte = bits,
                PageTraversal = traversal,
                Invert = _invert.IsChecked == true,
            });
        DialogResult = true;
    }

    private void UpdateSelection()
    {
        string error = _list.SelectedItem is Row row ? row.Error ?? string.Empty : string.Empty;
        _error.Text = error;
        _ok.IsEnabled = _list.SelectedItem is Row selected && selected.Error is null;
    }

    private static string Caption(ILocalizationService text, ImportedArray array)
    {
        string name = string.IsNullOrWhiteSpace(array.Name) ? text.Get("Import.Unnamed") : array.Name;
        return text.Format("Import.Array", name, array.Line, array.Values.Count);
    }

    private static ComboBox Combo<T>(IReadOnlyList<Labeled<T>> items, T selected)
        where T : notnull
    {
        return new ComboBox
        {
            ItemsSource = items,
            DisplayMemberPath = nameof(Labeled<T>.Caption),
            SelectedValuePath = nameof(Labeled<T>.Value),
            SelectedValue = selected,
            Margin = new Thickness(0, 4, 0, 0),
        };
    }

    private static Grid Field(string label, ComboBox combo)
    {
        var grid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        var caption = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(combo, 1);
        grid.Children.Add(caption);
        grid.Children.Add(combo);
        return grid;
    }

    private sealed class Row
    {
        public Row(string caption, string? error)
        {
            Caption = caption;
            Error = error;
        }

        public string Caption { get; }

        public string? Error { get; }
    }
}
