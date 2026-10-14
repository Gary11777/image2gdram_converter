using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace image2gdram_converter.Controls;

/// <summary>
/// По Enter и при потере фокуса сразу отдаёт текст привязки.
/// С командой — ещё и выполняет её: поля смещения так не ждут задержку 400 мс (N-65).
/// <see cref="FlushProperty"/> делает то же без команды, чтобы кнопки порога видели набранное число (N-68).
/// </summary>
public static class TextBoxCommit
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command",
        typeof(ICommand),
        typeof(TextBoxCommit),
        new PropertyMetadata(null, OnHookChanged));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached(
        "CommandParameter",
        typeof(object),
        typeof(TextBoxCommit),
        new PropertyMetadata(null));

    public static readonly DependencyProperty FlushProperty = DependencyProperty.RegisterAttached(
        "Flush",
        typeof(bool),
        typeof(TextBoxCommit),
        new PropertyMetadata(false, OnHookChanged));

    public static ICommand? GetCommand(DependencyObject element) => (ICommand?)element.GetValue(CommandProperty);

    public static void SetCommand(DependencyObject element, ICommand? value) => element.SetValue(CommandProperty, value);

    public static object? GetCommandParameter(DependencyObject element) => element.GetValue(CommandParameterProperty);

    public static void SetCommandParameter(DependencyObject element, object? value) =>
        element.SetValue(CommandParameterProperty, value);

    public static bool GetFlush(DependencyObject element) => (bool)element.GetValue(FlushProperty);

    public static void SetFlush(DependencyObject element, bool value) => element.SetValue(FlushProperty, value);

    public static void FlushFocused()
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA
            && Keyboard.FocusedElement is TextBox textBox
            && GetFlush(textBox))
        {
            BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty)?.UpdateSource();
        }
    }

    private static void OnHookChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
        {
            return;
        }

        textBox.LostKeyboardFocus -= OnLostKeyboardFocus;
        textBox.PreviewKeyDown -= OnPreviewKeyDown;
        if (GetCommand(textBox) is not null || GetFlush(textBox))
        {
            textBox.LostKeyboardFocus += OnLostKeyboardFocus;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
        }
    }

    private static void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            Commit(textBox);
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            Commit(textBox);
        }
    }

    private static void Commit(TextBox textBox)
    {
        BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty)?.UpdateSource();
        ICommand? command = GetCommand(textBox);
        if (command is null)
        {
            return;
        }

        object? parameter = GetCommandParameter(textBox);
        if (command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }
}
