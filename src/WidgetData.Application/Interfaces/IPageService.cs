using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IPageService
{
    Task<IEnumerable<PageDto>> GetAllAsync(int tenantId);
    Task<PageDto?> GetByIdAsync(int id);
    Task<PageDto?> GetBySlugAsync(string slug, int? tenantId = null);
    Task<PageDto> CreateAsync(CreatePageDto dto, int tenantId, string createdBy);
    Task<PageDto?> UpdateAsync(int id, UpdatePageDto dto);
    Task<bool> DeleteAsync(int id);
    Task AddWidgetAsync(int pageId, int widgetId, int position, int width);
    Task RemoveWidgetAsync(int pageId, int widgetId);
    Task UpdateWidgetLayoutAsync(int pageId, int widgetId, int position, int width);
}
