using System.Windows;
using System.Windows.Controls;

namespace image2gdram_converter.Services;

/// <summary> .     .</summary>
public sealed class DialogService : IDialogService
{
    private readonly ILocalizationService _text;

    public DialogService(ILocalizationService text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _text = text;
    }

    public bool Confirm(string message) => Show(message, null, null, ("Dialog.Yes", true), ("Dialog.No", false)) is true;

    public SaveChoice AskSave(string message)
    {
        SaveChoice? choice = Show(
            message,
            null,
            null,
            ("Dialog.Save", SaveChoice.Save),
            ("Dialog.Discard", SaveChoice.Discard),
            ("Dialog.Cancel", SaveChoice.Cancel));
        return choice ?? SaveChoice.Cancel;
    }

    public string? AskText(string message, string initial)
    {
        var box = new TextBox { Text = initial ?? string.Empty, MinWidth = 280, Margin = new Thickness(0, 8, 0, 0) };
        bool? accepted = Show(message, box, box, ("Dialog.Ok", true), ("Dialog.Cancel", false));
        return accepted == true ? box.Text : null;
    }

    public void Alert(string message) => Show(message, null, null, ("Dialog.Ok", true));

    private T? Show<T>(string message, TextBox? box, IInputElement? focus, params (string Key, T Result)[] buttons)
        where T : struct
    {
        var window = new Window
        {
            Title = _text.Get("Dialog.Title"),
            SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            MinWidth = 360,
            MaxWidth = 520,
        };
        Window? owner = Application.Current?.MainWindow;
        if (owner is { IsVisible: true })
        {
            window.Owner = owner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 460,
        });
        if (box is not null)
        {
            panel.Children.Add(box);
        }

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0),
        };
        T? result = null;
        foreach ((string key, T value) in buttons)
        {
            var button = new Button
            {
                Content = _text.Get(key),
                MinWidth = 100,
                Margin = new Thickness(8, 0, 0, 0),
                IsDefault = row.Children.Count == 0,
                IsCancel = key == "Dialog.Cancel" || key == "Dialog.No",
            };
            T captured = value;
            button.Click += (_, _) =>
            {
                result = captured;
                window.DialogResult = true;
            };
            row.Children.Add(button);
        }

        panel.Children.Add(row);
        window.Content = panel;
        if (focus is not null)
        {
            window.Loaded += (_, _) => focus.Focus();
        }

        bool? closed = window.ShowDialog();
        return closed == true ? result : null;
    }
}
