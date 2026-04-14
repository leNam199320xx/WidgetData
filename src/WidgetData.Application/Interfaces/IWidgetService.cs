using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IWidgetService
{
    Task<IEnumerable<WidgetDto>> GetAllAsync();
    Task<WidgetDto?> GetByIdAsync(int id);
    Task<WidgetDto> CreateAsync(CreateWidgetDto dto, string userId);
    Task<WidgetDto?> UpdateAsync(int id, UpdateWidgetDto dto);
    Task<bool> DeleteAsync(int id);
    Task<WidgetExecutionDto> ExecuteAsync(int id, string userId, int? scheduleId = null);
    Task<object?> GetDataAsync(int id);
    Task<IEnumerable<WidgetExecutionDto>> GetHistoryAsync(int id);
}
