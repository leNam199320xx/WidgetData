using System.Text.Json;
using Microsoft.Extensions.Logging;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class PageCrudService : IPageCrudService
{
    private readonly IPageRepository _repo;
    private readonly IPageVersioningService _versioning;
    private readonly ILogger _logger;

    public PageCrudService(IPageRepository repo, IPageVersioningService versioning, ILogger logger)
    {
        _repo = repo;
        _versioning = versioning;
        _logger = logger;
    }

    public async Task<IEnumerable<PageDto>> GetAllAsync(int? tenantId = null, ScreenType? screenType = null, bool includeWidgetContent = true)
    {
        var pages = await _repo.GetAllAsync(tenantId, screenType);
        return pages.Select(p => MapToDto(p, includeWidgetContent));
    }

    public async Task<PageDto?> GetByIdAsync(int id)
    {
        var page = await _repo.GetByIdAsync(id);
        return page == null ? null : MapToDto(page, includeWidgetContent: true);
    }

    public async Task<PageDto?> GetBySlugAsync(string slug, int? tenantId = null)
    {
        var page = await _repo.GetBySlugAsync(slug, tenantId);
        return page == null ? null : MapToDto(page, includeWidgetContent: true);
    }

    public async Task<PageDto> CreateAsync(CreatePageDto dto, int tenantId, string createdBy)
    {
        var page = new Page
        {
            TenantId = tenantId,
            Title = dto.Title,
            Slug = dto.Slug.ToLowerInvariant(),
            Description = dto.Description,
            ScreenType = dto.ScreenType,
            LifecycleState = ScreenLifecycleState.Draft,
            CurrentVersion = 1,
            IsActive = true,
            CreatedBy = createdBy
        };
        var created = await _repo.CreateAsync(page);
        await _versioning.SaveSnapshotAsync(created, createdBy, "DraftSaved", "Initial draft created");
        return MapToDto(created, includeWidgetContent: true);
    }

    public async Task<PageDto?> UpdateAsync(int id, UpdatePageDto dto)
    {
        var page = await _repo.GetByIdAsync(id);
        if (page == null) return null;

        page.Title = dto.Title;
        page.Slug = dto.Slug.ToLowerInvariant();
        page.Description = dto.Description;
        page.ScreenType = dto.ScreenType;
        page.LifecycleState = ScreenLifecycleState.Draft;
        page.IsActive = dto.IsActive;
        page.CurrentVersion++;
        var updated = await _repo.UpdateAsync(page);
        await _versioning.SaveSnapshotAsync(updated, updated.CreatedBy ?? "system", "DraftSaved", "Draft updated");
        return MapToDto(updated, includeWidgetContent: true);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var page = await _repo.GetByIdAsync(id);
        if (page == null) return false;
        await _repo.DeleteAsync(id);
        return true;
    }

    private static PageDto MapToDto(Page page, bool includeWidgetContent) => new()
    {
        Id = page.Id,
        TenantId = page.TenantId,
        Title = page.Title,
        Slug = page.Slug,
        Description = page.Description,
        ScreenType = page.ScreenType,
        LifecycleState = page.LifecycleState,
        CurrentVersion = page.CurrentVersion,
        PublishedAt = page.PublishedAt,
        PublishedBy = page.PublishedBy,
        IsActive = page.IsActive,
        CreatedBy = page.CreatedBy,
        CreatedAt = page.CreatedAt,
        Widgets = page.PageWidgets
            .OrderBy(pw => pw.Position)
            .Select(pw => new PageWidgetDto
            {
                Id = pw.Id,
                WidgetId = pw.WidgetId,
                WidgetName = pw.Widget?.Name ?? string.Empty,
                FriendlyLabel = pw.Widget?.FriendlyLabel,
                HtmlTemplate = includeWidgetContent ? pw.Widget?.HtmlTemplate : null,
                Configuration = includeWidgetContent ? pw.Widget?.Configuration : null,
                WidgetType = pw.Widget?.WidgetType.ToString() ?? string.Empty,
                Position = pw.Position,
                Width = pw.Width
            }).ToList()
    };
}