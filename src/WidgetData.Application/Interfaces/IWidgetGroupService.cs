using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IWidgetGroupService
{
    Task<IEnumerable<WidgetGroupDto>> GetAllAsync();
    Task<WidgetGroupDto?> GetByIdAsync(int id);
    Task<WidgetGroupDto> CreateAsync(CreateWidgetGroupDto dto, string userId);
    Task<WidgetGroupDto?> UpdateAsync(int id, UpdateWidgetGroupDto dto);
    Task<bool> DeleteAsync(int id);
}
