using Cronos;

namespace WidgetData.CrossCutting.Helpers;

public static class CronUtils
{
    public static DateTime? GetNextOccurrence(string cronExpression, string timezone, DateTime? from = null)
    {
        try
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.Standard);
            var tz = GetTimeZone(timezone);
            var fromUtc = from ?? DateTime.UtcNow;
            return expression.GetNextOccurrence(fromUtc, tz);
        }
        catch
        {
            return null;
        }
    }

    public static bool IsValid(string cronExpression)
    {
        try
        {
            CronExpression.Parse(cronExpression, CronFormat.Standard);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static TimeZoneInfo GetTimeZone(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone) || timezone == "UTC")
            return TimeZoneInfo.Utc;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}
