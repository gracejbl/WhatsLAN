using System.Windows;
using WhatsLAN.App.MVVM;

namespace WhatsLAN.App.Services;

public class ThemeManager : ObservableObject
{
    private static ThemeManager? _instance;
    public static ThemeManager Instance => _instance ??= new ThemeManager();

    private bool _isDarkTheme = true; // Dark theme by default as per WhatsApp Desktop reference notes

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                ApplyTheme(value);
            }
        }
    }

    public void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    public void ApplyTheme(bool isDark)
    {
        var app = Application.Current;
        if (app == null) return;

        var themeUri = isDark
            ? new Uri("Themes/Dark.xaml", UriKind.Relative)
            : new Uri("Themes/Light.xaml", UriKind.Relative);

        var newDict = new ResourceDictionary { Source = themeUri };

        // Replace the active theme dictionary (index 0)
        var merged = app.Resources.MergedDictionaries;
        for (int i = 0; i < merged.Count; i++)
        {
            var src = merged[i].Source?.OriginalString;
            if (src != null && (src.Contains("Dark.xaml") || src.Contains("Light.xaml")))
            {
                merged[i] = newDict;
                return;
            }
        }

        merged.Insert(0, newDict);
    }
}
