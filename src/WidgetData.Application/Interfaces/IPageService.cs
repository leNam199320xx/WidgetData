using WidgetData.Application.DTOs;
using WidgetData.Domain.Enums;

namespace WidgetData.Application.Interfaces;

public interface IPageService
{
    Task<IEnumerable<PageDto>> GetAllAsync(
        int? tenantId = null,
        ScreenType? screenType = null,
        bool includeWidgetContent = true);
    Task<PageDto?> GetByIdAsync(int id);
    Task<PageDto?> GetBySlugAsync(string slug, int? tenantId = null);
    Task<PageDto> CreateAsync(CreatePageDto dto, int tenantId, string createdBy);
    Task<PageDto?> UpdateAsync(int id, UpdatePageDto dto);
    Task<PageDto?> PublishAsync(int id, string userId, PublishPageDto? dto = null);
    Task<PageDto?> RollbackAsync(int id, int versionNumber, string userId, RollbackPageDto? dto = null);
    Task<IEnumerable<PageVersionDto>> GetVersionsAsync(int id);
    Task<bool> DeleteAsync(int id);
    Task AddWidgetAsync(int pageId, int widgetId, int position, int width);
    Task RemoveWidgetAsync(int pageId, int widgetId);
    Task UpdateWidgetLayoutAsync(int pageId, int widgetId, int position, int width);
}
