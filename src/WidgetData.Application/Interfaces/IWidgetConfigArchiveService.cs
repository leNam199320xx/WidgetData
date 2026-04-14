using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IWidgetConfigArchiveService
{
    Task<IEnumerable<WidgetConfigArchiveDto>> GetAllAsync();
    Task<IEnumerable<WidgetConfigArchiveDto>> GetByWidgetIdAsync(int widgetId);
    Task<WidgetConfigArchiveDto?> GetByIdAsync(int id);
    Task<WidgetConfigArchiveDto> CreateAsync(CreateWidgetConfigArchiveDto dto, string archivedBy);
    Task<WidgetConfigArchiveDto?> CreateForScheduleAsync(int widgetId, int scheduleId, string archivedBy);
    Task<bool> RestoreAsync(int archiveId);
    Task<bool> DeleteAsync(int id);
}
