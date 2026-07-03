using WidgetData.Application.DTOs;
using WidgetData.Domain.Entities;

namespace WidgetData.Application.Interfaces;

public interface IWidgetCrudService
{
    Task<IEnumerable<WidgetDto>> GetAllAsync();
    Task<WidgetDto?> GetByIdAsync(int id);
    Task<WidgetDto> CreateAsync(CreateWidgetDto dto, string userId);
    Task<WidgetDto?> UpdateAsync(int id, UpdateWidgetDto dto);
    Task<bool> DeleteAsync(int id);
}