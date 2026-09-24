using System.Collections.Specialized;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace FoxIRCClient.Behaviors;

public class AutoSelectTabBehavior
{
    public static readonly DependencyProperty EnableAutoSelectProperty =
        DependencyProperty.RegisterAttached(
            "EnableAutoSelect",
            typeof(bool),
            typeof(AutoSelectTabBehavior),
            new PropertyMetadata(false, OnEnableAutoSelectChanged));

    public static bool GetEnableAutoSelect(DependencyObject obj) => (bool)obj.GetValue(EnableAutoSelectProperty);
    public static void SetEnableAutoSelect(DependencyObject obj, bool value) => obj.SetValue(EnableAutoSelectProperty, value);

    private static void OnEnableAutoSelectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TabControl tabControl)
        {
            if ((bool)e.NewValue)
                tabControl.Loaded += TabControl_Loaded;
            else
                tabControl.Loaded -= TabControl_Loaded;
        }
    }

    private static void TabControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TabControl tabControl && tabControl.ItemsSource is INotifyCollectionChanged colledtion)
        {
            colledtion.CollectionChanged += (s, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add && args.NewItems != null)
                {
                    foreach (var item in args.NewItems)
                    {
                        PropertyInfo? prop = item.GetType().GetProperty("ChannelId");
                        string? channelId = prop?.GetValue(item)?.ToString();

                        if (!string.IsNullOrEmpty(channelId) && channelId.StartsWith('#'))
                        {
                            tabControl.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                tabControl.SelectedItem = item;
                            }));
                            break;
                        }
                    }
                }
            };
        }
    }
}
