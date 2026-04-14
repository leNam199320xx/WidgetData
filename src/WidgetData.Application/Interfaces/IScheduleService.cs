using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IScheduleService
{
    Task<IEnumerable<WidgetScheduleDto>> GetAllAsync();
    Task<WidgetScheduleDto> CreateAsync(CreateScheduleDto dto);
    Task<WidgetScheduleDto?> UpdateAsync(int id, UpdateScheduleDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> EnableAsync(int id);
    Task<bool> DisableAsync(int id);
    Task<WidgetScheduleDto?> TriggerAsync(int id, string triggeredBy);
}
