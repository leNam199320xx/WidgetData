using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace WidgetData.Infrastructure.Services;

public class PageService : IPageService
{
    private readonly IPageCrudService _crud;
    private readonly IPageVersioningService _versioning;
    private readonly IPageLayoutService _layout;

    public PageService(IPageCrudService crud, IPageVersioningService versioning, IPageLayoutService layout)
    {
        _crud = crud;
        _versioning = versioning;
        _layout = layout;
    }

    public PageService(IPageRepository repo)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var versioningLogger = loggerFactory.CreateLogger<PageVersioningService>();
        var crudLogger = loggerFactory.CreateLogger<PageCrudService>();
        var layoutLogger = loggerFactory.CreateLogger<PageLayoutService>();
        _versioning = new PageVersioningService(repo, versioningLogger);
        _crud = new PageCrudService(repo, _versioning, crudLogger);
        _layout = new PageLayoutService(repo, layoutLogger);
    }

    public Task<IEnumerable<PageDto>> GetAllAsync(int? tenantId = null, ScreenType? screenType = null, bool includeWidgetContent = true)
        => _crud.GetAllAsync(tenantId, screenType, includeWidgetContent);
    public Task<PageDto?> GetByIdAsync(int id) => _crud.GetByIdAsync(id);
    public Task<PageDto?> GetBySlugAsync(string slug, int? tenantId = null) => _crud.GetBySlugAsync(slug, tenantId);
    public Task<PageDto> CreateAsync(CreatePageDto dto, int tenantId, string createdBy) => _crud.CreateAsync(dto, tenantId, createdBy);
    public Task<PageDto?> UpdateAsync(int id, UpdatePageDto dto) => _crud.UpdateAsync(id, dto);
    public Task<PageDto?> PublishAsync(int id, string userId, PublishPageDto? dto = null) => _versioning.PublishAsync(id, userId, dto);
    public Task<PageDto?> RollbackAsync(int id, int versionNumber, string userId, RollbackPageDto? dto = null) => _versioning.RollbackAsync(id, versionNumber, userId, dto);
    public Task<IEnumerable<PageVersionDto>> GetVersionsAsync(int id) => _versioning.GetVersionsAsync(id);
    public Task<bool> DeleteAsync(int id) => _crud.DeleteAsync(id);
    public Task AddWidgetAsync(int pageId, int widgetId, int position, int width) => _layout.AddWidgetAsync(pageId, widgetId, position, width);
    public Task RemoveWidgetAsync(int pageId, int widgetId) => _layout.RemoveWidgetAsync(pageId, widgetId);
    public Task UpdateWidgetLayoutAsync(int pageId, int widgetId, int position, int width) => _layout.UpdateWidgetLayoutAsync(pageId, widgetId, position, width);
}