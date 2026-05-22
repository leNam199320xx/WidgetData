using WidgetData.Domain.Entities;

namespace WidgetData.Domain.Interfaces;

public interface IScheduleRepository
{
    Task<IEnumerable<WidgetSchedule>> GetAllAsync();
    Task<int> CountAsync();
    Task<int> CountEnabledAsync();
    Task<(int Total, int Enabled)> GetCountsAsync();
    Task<IEnumerable<WidgetSchedule>> GetByWidgetIdAsync(int widgetId);
    Task<WidgetSchedule?> GetByIdAsync(int id);
    Task<IEnumerable<WidgetSchedule>> GetDueAsync(DateTime asOf);
    Task<WidgetSchedule> CreateAsync(WidgetSchedule schedule);
    Task<WidgetSchedule> UpdateAsync(WidgetSchedule schedule);
    Task DeleteAsync(int id);
}
