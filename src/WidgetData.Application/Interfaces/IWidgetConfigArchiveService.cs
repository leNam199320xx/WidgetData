using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IWidgetConfigArchiveService
{
    Task<IEnumerable<WidgetConfigArchiveDto>> GetByWidgetIdAsync(int widgetId);
    Task<WidgetConfigArchiveDto?> CreateAsync(int widgetId, CreateWidgetConfigArchiveDto dto, string userId,
        string triggerSource = "Manual", int? scheduleId = null);
    Task<WidgetDto?> RestoreAsync(int widgetId, int archiveId, string userId);
    Task<bool> DeleteAsync(int archiveId);
}
