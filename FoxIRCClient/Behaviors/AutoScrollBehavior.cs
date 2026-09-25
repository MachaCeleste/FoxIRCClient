using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FoxIRCClient.Behaviors;

public static class AutoScrollBehavior
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
        if (d is ListView listView && (bool)e.NewValue)
            listView.Loaded += ListView_Loaded;
    }

    private static void ListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listView && listView.Items is INotifyCollectionChanged items)
        {
            NotifyCollectionChangedEventHandler handler = (s, args) => ScrollToBottom(listView);

            items.CollectionChanged += handler;
            listView.Unloaded += (s, args) => items.CollectionChanged -= handler;

            ScrollToBottom(listView);
        }
    }

    private static void ScrollToBottom(ListView listView)
    {
        if (listView.ItemsSource == null || listView.Items.Count == 0) return;

            listView.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                try
                {
                    int count = listView.Items.Count;
                    if (count > 0)
                    {
                        var listItem = listView.Items[count - 1];

                        if (listItem != null)
                            listView.ScrollIntoView(listItem);
                    }
                }
                catch (InvalidOperationException) { }
            });
    }
}
