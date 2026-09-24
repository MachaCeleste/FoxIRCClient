using System.Windows;

namespace FoxIRCClient.Utils;

public static class ThemeManager
{
    public static void ChangeTheme(string themeName)
    {
        var appResources = Application.Current.Resources.MergedDictionaries;
        var newThemeDict = new ResourceDictionary
        {
            Source = new Uri($"Themes/{themeName}.xaml", UriKind.Relative)
        };

        var currentThemeDict = appResources.FirstOrDefault(x =>
        x.Source != null && x.Source.OriginalString.StartsWith("Themes/"));

        if (currentThemeDict != null)
            appResources.Remove(currentThemeDict);

        appResources.Add(newThemeDict);
    }
}
