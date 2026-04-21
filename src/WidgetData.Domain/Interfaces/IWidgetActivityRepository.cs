using WidgetData.Domain.Entities;

namespace WidgetData.Domain.Interfaces;

public interface IWidgetActivityRepository
{
    Task<WidgetApiActivity> RecordAsync(WidgetApiActivity activity);
    Task<IEnumerable<WidgetApiActivity>> GetByWidgetIdAsync(int widgetId, int page, int pageSize);
    Task<int> CountByWidgetIdAsync(int widgetId);
    Task<IEnumerable<WidgetApiActivity>> GetSummaryDataAsync(int widgetId);
    Task<IEnumerable<Widget>> GetInactiveWidgetsAsync(int thresholdDays);
}
