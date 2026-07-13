using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace WidgetData.Pages;

internal static class FileBackedRepositoryId
{
    public static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}

public class FileBackedPageRepository : IPageRepository
{
    private readonly IJsonPageRepository _pageRepo;
    private readonly IJsonPageVersionRepository _versionRepo;
    private readonly IJsonPageWidgetRepository _pageWidgetRepo;
    private readonly IJsonWidgetRepository _widgetRepo;
    private readonly ITenantContext? _tenantContext;

    public FileBackedPageRepository(
        IJsonPageRepository pageRepo,
        IJsonPageVersionRepository versionRepo,
        IJsonPageWidgetRepository pageWidgetRepo,
        IJsonWidgetRepository widgetRepo,
        ITenantContext? tenantContext = null)
    {
        _pageRepo = pageRepo;
        _versionRepo = versionRepo;
        _pageWidgetRepo = pageWidgetRepo;
        _widgetRepo = widgetRepo;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<Page>> GetAllAsync(int? tenantId = null, ScreenType? screenType = null)
    {
        var pages = await _pageRepo.GetAllAsync();

        IEnumerable<Page> filtered = pages;
        if (tenantId.HasValue)
            filtered = filtered.Where(p => p.TenantId == tenantId.Value);
        else if (!(_tenantContext?.IsSuperAdmin == true || _tenantContext?.CurrentTenantId == null))
            filtered = filtered.Where(p => p.TenantId == _tenantContext!.CurrentTenantId);

        if (screenType.HasValue)
            filtered = filtered.Where(p => p.ScreenType == screenType.Value);

        var result = filtered.OrderBy(p => p.Title).ToList();
        foreach (var page in result)
            await LoadWidgetsAsync(page);

        return result;
    }

    public async Task<Page?> GetByIdAsync(int id)
    {
        var page = await _pageRepo.GetByIdAsync(id);
        if (page == null) return null;
        await LoadWidgetsAsync(page);
        return page;
    }

    public async Task<Page?> GetBySlugAsync(string slug, int? tenantId = null)
    {
        var pages = await GetAllAsync(tenantId);
        var page = pages.FirstOrDefault(p => p.Slug == slug && p.IsActive);
        if (page == null) return null;
        await LoadWidgetsAsync(page);
        return page;
    }

    public async Task<Page> CreateAsync(Page page)
    {
        var all = await _pageRepo.GetAllAsync();
        page.Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id));
        return await _pageRepo.CreateAsync(page);
    }

    public Task<Page> UpdateAsync(Page page) => _pageRepo.UpdateAsync(page);

    public async Task<PageVersion> CreateVersionAsync(PageVersion pageVersion)
    {
        var all = await _versionRepo.GetAllAsync();
        pageVersion.Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id));
        return await _versionRepo.CreateAsync(pageVersion);
    }

    public async Task<PageVersion?> GetVersionAsync(int pageId, int versionNumber)
    {
        var versions = await _versionRepo.GetByPageAsync(pageId);
        return versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
    }

    public async Task<IEnumerable<PageVersion>> GetVersionsAsync(int pageId)
        => await _versionRepo.GetByPageAsync(pageId);

    public async Task DeleteAsync(int id)
    {
        await _pageRepo.DeleteAsync(id);
        var versions = await _versionRepo.GetByPageAsync(id);
        foreach (var version in versions)
            await _versionRepo.DeleteAsync(version.Id);

        var widgets = await _pageWidgetRepo.GetByPageAsync(id);
        foreach (var widget in widgets)
            await _pageWidgetRepo.DeleteAsync(widget.Id);
    }

    public async Task AddWidgetAsync(int pageId, int widgetId, int position, int width)
    {
        var existing = (await _pageWidgetRepo.GetByPageAsync(pageId))
            .FirstOrDefault(pw => pw.WidgetId == widgetId);

        if (existing != null)
        {
            existing.Position = position;
            existing.Width = width;
            await _pageWidgetRepo.UpdateAsync(existing);
            return;
        }

        var all = await _pageWidgetRepo.GetAllAsync();
        await _pageWidgetRepo.CreateAsync(new PageWidget
        {
            Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id)),
            PageId = pageId,
            WidgetId = widgetId,
            Position = position,
            Width = width
        });
    }

    public async Task RemoveWidgetAsync(int pageId, int widgetId)
    {
        var existing = (await _pageWidgetRepo.GetByPageAsync(pageId))
            .FirstOrDefault(pw => pw.WidgetId == widgetId);
        if (existing != null)
            await _pageWidgetRepo.DeleteAsync(existing.Id);
    }

    public async Task UpdateWidgetLayoutAsync(int pageId, int widgetId, int position, int width)
    {
        var existing = (await _pageWidgetRepo.GetByPageAsync(pageId))
            .FirstOrDefault(pw => pw.WidgetId == widgetId);
        if (existing != null)
        {
            existing.Position = position;
            existing.Width = width;
            await _pageWidgetRepo.UpdateAsync(existing);
        }
    }

    private async Task LoadWidgetsAsync(Page page)
    {
        var links = await _pageWidgetRepo.GetByPageAsync(page.Id);
        var widgets = await _widgetRepo.GetAllAsync();
        var widgetMap = widgets.ToDictionary(w => w.Id);

        page.PageWidgets = links
            .OrderBy(x => x.Position)
            .Select(link =>
            {
                if (widgetMap.TryGetValue(link.WidgetId, out var widget))
                    link.Widget = widget;
                return link;
            })
            .ToList();
    }
}
