using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace FoxIRCClient;

public static class AutoScrollBehavior // TODO system not remembering position on tabs that arent focused
{
    public static readonly DependencyProperty AutoScrollProperty =
        DependencyProperty.RegisterAttached(
            "AutoScroll",
            typeof(bool),
            typeof(AutoScrollBehavior),
            new PropertyMetadata(false, OnAutoScrollChanged));

    public static bool GetAutoScroll(DependencyObject obj) => (bool)obj.GetValue(AutoScrollProperty);
    public static void SetAutoScroll(DependencyObject obj, bool value) => obj.SetValue(AutoScrollProperty, value);

    private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListView listView)
        {
            if ((bool)e.NewValue)
            {
                listView.Loaded += ListView_Loaded;
                listView.Unloaded += ListView_Unloaded;
            }
            else
            {
                listView.Loaded -= ListView_Loaded;
                listView.Unloaded -= ListView_Unloaded;
            }
        }
    }

    private static void ListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listView && listView.ItemsSource is INotifyCollectionChanged notifyCollection)
        {
            notifyCollection.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add && listView.Items.Count > 0)
                    listView.Dispatcher.InvokeAsync(() =>
                    {
                        int count = listView.Items.Count;
                        if (count > 0)
                        {
                            var lastItem = listView.Items[count - 1];
                            listView.ScrollIntoView(lastItem);
                        }
                    });
            };
        }
    }

    private static void ListView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listView && listView.ItemsSource is INotifyCollectionChanged notifyCollection)
            notifyCollection.CollectionChanged -= (s, args) => { };
    }
}
