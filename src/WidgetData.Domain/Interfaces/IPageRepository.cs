using WidgetData.Domain.Enums;
using WidgetData.Domain.Entities;

namespace WidgetData.Domain.Interfaces;

public interface IPageRepository
{
    Task<IEnumerable<Page>> GetAllAsync(int? tenantId = null, ScreenType? screenType = null);
    Task<Page?> GetByIdAsync(int id);
    Task<Page?> GetBySlugAsync(string slug, int? tenantId = null);
    Task<Page> CreateAsync(Page page);
    Task<Page> UpdateAsync(Page page);
    Task<PageVersion> CreateVersionAsync(PageVersion pageVersion);
    Task<PageVersion?> GetVersionAsync(int pageId, int versionNumber);
    Task<IEnumerable<PageVersion>> GetVersionsAsync(int pageId);
    Task DeleteAsync(int id);
    Task AddWidgetAsync(int pageId, int widgetId, int position, int width);
    Task RemoveWidgetAsync(int pageId, int widgetId);
    Task UpdateWidgetLayoutAsync(int pageId, int widgetId, int position, int width);
}
