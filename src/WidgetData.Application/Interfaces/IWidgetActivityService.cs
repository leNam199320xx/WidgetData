using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IWidgetActivityService
{
    Task RecordAsync(int widgetId, string apiEndpoint, string? userId, long? responseTimeMs, int statusCode);
    Task<PagedResult<WidgetActivityDto>> GetActivityAsync(int widgetId, int page, int pageSize);
    Task<WidgetActivitySummaryDto?> GetSummaryAsync(int widgetId);
    Task<IEnumerable<InactivityAlertDto>> GetInactiveWidgetsAsync(int thresholdDays);
    Task<IEnumerable<InactivityAlertDto>> GetInactivityAlertsAsync();
}
