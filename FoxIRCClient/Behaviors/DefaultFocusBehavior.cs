using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace FoxIRCClient.Behaviors;

public static class DefaultFocusBehavior
{
    public static readonly DependencyProperty IsFocusedProperty =
        DependencyProperty.RegisterAttached(
            "IsFocused",
            typeof(bool),
            typeof(DefaultFocusBehavior),
            new UIPropertyMetadata(false, OnIsFocusedChanged));

    public static bool GetIsFocused(DependencyObject obj) => (bool)obj.GetValue(IsFocusedProperty);
    public static void SetIsFocused(DependencyObject obj, bool value) => obj.SetValue(IsFocusedProperty, value);

    private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element && e.NewValue is bool isFocused && isFocused)
            element.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                element.Focus();
                Keyboard.Focus(element);
            }));
    }
}
