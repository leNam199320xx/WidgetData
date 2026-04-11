using WidgetData.Domain.Entities;

namespace WidgetData.Domain.Interfaces;

public interface IScheduleRepository
{
    Task<IEnumerable<WidgetSchedule>> GetAllAsync();
    Task<IEnumerable<WidgetSchedule>> GetByWidgetIdAsync(int widgetId);
    Task<WidgetSchedule?> GetByIdAsync(int id);
    Task<WidgetSchedule> CreateAsync(WidgetSchedule schedule);
    Task<WidgetSchedule> UpdateAsync(WidgetSchedule schedule);
    Task DeleteAsync(int id);
}
