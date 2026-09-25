using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace FoxIRCClient.Utils;

public static class ThemeManager
{
    private static readonly string themeDir = Path.Combine(DataManager.GetFilePath(), "Themes");

    public static ObservableCollection<string> CustomThemes { get; } = [];

    public static void OpenThemesFolder()
    {
        SetupThemesFolder();

        Process.Start(new ProcessStartInfo
        {
            FileName = themeDir,
            UseShellExecute = true
        });
    }

    public static void ReloadCustomThemes()
    {
        CustomThemes.Clear();

        SetupThemesFolder();

        var themeNames = Directory.GetFiles(themeDir, "*.xaml")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name) && name != "Template");

        foreach (var theme in themeNames)
            CustomThemes.Add(theme!);
    }

    public static void ApplyTheme(string themeName)
    {
        DataManager.CurrentTheme = themeName;

        string customFilePath = Path.Combine(themeDir, $"{themeName}.xaml");
        if (File.Exists(customFilePath))
        {
            ApplyCustomTheme(customFilePath);
            return;
        }

        ApplyBuiltInTheme(themeName);
    }

    private static void ApplyCustomTheme(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (XamlReader.Load(stream) is ResourceDictionary customDict)
                ReplaceThemeDict(customDict);
        }
        catch (Exception) { }

        DataManager.SaveThemeConfig();
    }

    private static void ApplyBuiltInTheme(string themeName)
    {
        var uri = new Uri($"Themes/{themeName}.xaml", UriKind.Relative);

        try
        {
            var streamInfo = Application.GetResourceStream(uri);
        }
        catch
        {
            uri = new Uri($"Themes/Cynical.xaml", UriKind.Relative);
            DataManager.CurrentTheme = "Cynical";
        }

        try
        {
            var resourceDict = new ResourceDictionary { Source = uri };
            ReplaceThemeDict(resourceDict);
        }
        catch (Exception) { }

        DataManager.SaveThemeConfig();
    }

    private static void SetupThemesFolder()
    {
        if (!Directory.Exists(themeDir))
            Directory.CreateDirectory(themeDir);

        string examplePath = Path.Combine(themeDir, "Template.xaml");
        if (!File.Exists(examplePath))
        {
            var assembly = typeof(ThemeManager).Assembly;

            string? resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(x => x.EndsWith("ExampleTheme.xaml", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(resourceName))
            {
                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using FileStream fileStream = new FileStream(examplePath, FileMode.Create, FileAccess.Write);
                    stream.CopyTo(fileStream);
                }
            }
        }
    }

    private static void ReplaceThemeDict(ResourceDictionary newDict)
    {
        var merged = Application.Current.Resources.MergedDictionaries;
        merged.Clear();
        merged.Add(newDict);
    }
}
