using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WhatsLAN.App.Converters;

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not Visibility.Visible;
    }
}

public class StringEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return Visibility.Collapsed;
        if (value is string s && string.IsNullOrWhiteSpace(s)) return Visibility.Collapsed;
        if (value is int i && i == 0) return Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as string;
        var app = Application.Current;
        if (app == null) return null;

        return status?.ToLowerInvariant() switch
        {
            "pending" => app.TryFindResource("IconClock"),
            "sent" => app.TryFindResource("IconCheck"),
            "delivered" => app.TryFindResource("IconDoubleCheck"),
            "read" => app.TryFindResource("IconDoubleCheck"),
            "failed" => app.TryFindResource("IconFailed"),
            _ => app.TryFindResource("IconCheck")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TicksColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as string;
        if (string.Equals(status, "Read", StringComparison.OrdinalIgnoreCase))
        {
            return Application.Current.TryFindResource("TickBlue") ?? System.Windows.Media.Brushes.DeepSkyBlue;
        }

        if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return Application.Current.TryFindResource("Danger") ?? System.Windows.Media.Brushes.Red;
        }

        return Application.Current.TryFindResource("TextSecondary") ?? System.Windows.Media.Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
