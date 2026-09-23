using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FoxIRCClient;

public static class EnterKeyBehavior
{
    public static readonly DependencyProperty ExecuteCommandOnEnterProperty =
        DependencyProperty.RegisterAttached(
            "ExecuteCommandOnEnter",
            typeof(ICommand),
            typeof(EnterKeyBehavior),
            new PropertyMetadata(null, OnExecuteCommandOnEnterChange));

    public static ICommand GetExecuteCommandOnEnter(DependencyObject obj) =>
        (ICommand)obj.GetValue(ExecuteCommandOnEnterProperty);

    public static void SetExecuteCommandOnEnter(DependencyObject obj, ICommand value) =>
        obj.SetValue(ExecuteCommandOnEnterProperty, value);

    private static void OnExecuteCommandOnEnterChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
            if (e.NewValue != null)
                textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
        }
    }

    private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                return;

            var textBox = (TextBox)sender;
            var command = GetExecuteCommandOnEnter(textBox);

            if (command != null && command.CanExecute(null))
            {
                command.Execute(null);
                e.Handled = true;
            }
        }
    }
}
