using Cronos;

namespace WidgetData.Infrastructure.Helpers;

/// <summary>
/// Tiện ích tính thời điểm chạy tiếp theo từ cron expression và timezone.
/// </summary>
public static class CronUtils
{
    /// <summary>
    /// Tính DateTime UTC của lần chạy tiếp theo sau <paramref name="from"/>.
    /// Trả về null nếu cron expression không hợp lệ hoặc không có lần chạy tiếp theo.
    /// </summary>
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

    /// <summary>
    /// Kiểm tra cron expression có hợp lệ không.
    /// </summary>
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
