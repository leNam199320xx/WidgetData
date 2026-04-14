using WidgetData.Domain.Entities;

namespace WidgetData.Domain.Interfaces;

public interface IWidgetConfigArchiveRepository
{
    Task<IEnumerable<WidgetConfigArchive>> GetAllAsync();
    Task<IEnumerable<WidgetConfigArchive>> GetByWidgetIdAsync(int widgetId);
    Task<WidgetConfigArchive?> GetByIdAsync(int id);
    Task<WidgetConfigArchive> CreateAsync(WidgetConfigArchive archive);
    Task<bool> DeleteAsync(int id);
}
