using FoxIRCClient.ViewModels;
using System.Collections.Specialized;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace FoxIRCClient.Behaviors;

public class SelectNewTabBehavior
{
    public static readonly DependencyProperty EnableAutoSelectProperty =
        DependencyProperty.RegisterAttached(
            "EnableAutoSelect",
            typeof(bool),
            typeof(SelectNewTabBehavior),
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
        if (sender is TabControl tabControl && tabControl.ItemsSource is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += (s, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add && args.NewItems != null)
                {
                    foreach (var item in args.NewItems)
                    {
                        Type typeInfo = item.GetType();

                        string propName = "ChannelId";
                        if (typeInfo == typeof(ServerViewModel))
                            propName = "ServerAddress";

                        PropertyInfo? prop = typeInfo.GetProperty(propName);
                        string? tabId = prop?.GetValue(item)?.ToString();

                        if (!string.IsNullOrEmpty(tabId) && (tabId.StartsWith('#') ||
                        tabId.Equals("System", StringComparison.OrdinalIgnoreCase) ||
                        propName.Equals("ServerAddress", StringComparison.OrdinalIgnoreCase)))
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
