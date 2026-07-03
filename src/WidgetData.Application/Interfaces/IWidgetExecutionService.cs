using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IWidgetExecutionService
{
    Task<WidgetExecutionDto> ExecuteAsync(int id, string userId, int? scheduleId = null);
    Task<object?> GetDataAsync(int id);
    Task<IEnumerable<WidgetExecutionDto>> GetHistoryAsync(int id);
}