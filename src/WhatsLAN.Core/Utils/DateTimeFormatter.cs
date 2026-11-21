using System.Globalization;

namespace WhatsLAN.Core.Utils;

public static class DateTimeFormatter
{
    private static readonly CultureInfo FrCulture = new("fr-FR");

    public static string FormatChatListTime(DateTime utcTime, DateTime? relativeNow = null)
    {
        var local = utcTime.ToLocalTime();
        var today = (relativeNow?.ToLocalTime() ?? DateTime.Now).Date;
        var messageDate = local.Date;

        if (messageDate == today)
        {
            return local.ToString("HH:mm", FrCulture);
        }

        if (messageDate == today.AddDays(-1))
        {
            return "Hier";
        }

        if (today - messageDate < TimeSpan.FromDays(7) && messageDate.DayOfWeek != today.DayOfWeek)
        {
            var dayName = local.ToString("dddd", FrCulture);
            return char.ToUpper(dayName[0]) + dayName[1..];
        }

        return local.ToString("dd/MM/yyyy", FrCulture);
    }

    public static string FormatDateSeparator(DateTime utcTime, DateTime? relativeNow = null)
    {
        var local = utcTime.ToLocalTime();
        var today = (relativeNow?.ToLocalTime() ?? DateTime.Now).Date;
        var messageDate = local.Date;

        if (messageDate == today)
        {
            return "AUJOURD'HUI";
        }

        if (messageDate == today.AddDays(-1))
        {
            return "HIER";
        }

        if (today - messageDate < TimeSpan.FromDays(7))
        {
            return local.ToString("dddd", FrCulture).ToUpper(FrCulture);
        }

        return local.ToString("d MMMM yyyy", FrCulture);
    }

    public static string FormatLastSeen(bool isOnline, DateTime? lastSeenUtc = null, DateTime? relativeNow = null)
    {
        if (isOnline)
        {
            return "en ligne";
        }

        if (!lastSeenUtc.HasValue)
        {
            return "hors ligne";
        }

        var local = lastSeenUtc.Value.ToLocalTime();
        var today = (relativeNow?.ToLocalTime() ?? DateTime.Now).Date;
        var date = local.Date;

        if (date == today)
        {
            return $"vu aujourd’hui à {local:HH:mm}";
        }

        if (date == today.AddDays(-1))
        {
            return $"vu hier à {local:HH:mm}";
        }

        return $"vu le {local:dd/MM/yyyy} à {local:HH:mm}";
    }
}
