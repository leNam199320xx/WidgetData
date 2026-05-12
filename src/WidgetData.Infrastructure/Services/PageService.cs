using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using System.Text.Json;

namespace WidgetData.Infrastructure.Services;

public class PageService : IPageService
{
    private readonly IPageRepository _repo;

    public PageService(IPageRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<PageDto>> GetAllAsync(int? tenantId = null, ScreenType? screenType = null)
    {
        var pages = await _repo.GetAllAsync(tenantId, screenType);
        return pages.Select(MapToDto);
    }

    public async Task<PageDto?> GetByIdAsync(int id)
    {
        var page = await _repo.GetByIdAsync(id);
        return page == null ? null : MapToDto(page);
    }

    public async Task<PageDto?> GetBySlugAsync(string slug, int? tenantId = null)
    {
        var page = await _repo.GetBySlugAsync(slug, tenantId);
        return page == null ? null : MapToDto(page);
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
        await SaveVersionAsync(created, createdBy, "DraftSaved", "Initial draft created");
        return MapToDto(created);
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
        await SaveVersionAsync(updated, updated.CreatedBy ?? "system", "DraftSaved", "Draft updated");
        return MapToDto(updated);
    }

    public async Task<PageDto?> PublishAsync(int id, string userId, PublishPageDto? dto = null)
    {
        var page = await _repo.GetByIdAsync(id);
        if (page == null) return null;

        page.LifecycleState = ScreenLifecycleState.Published;
        page.PublishedAt = DateTime.UtcNow;
        page.PublishedBy = userId;
        page.CurrentVersion++;

        var updated = await _repo.UpdateAsync(page);
        await SaveVersionAsync(updated, userId, "Published", dto?.Note ?? "Published screen");
        return MapToDto(updated);
    }

    public async Task<PageDto?> RollbackAsync(int id, int versionNumber, string userId, RollbackPageDto? dto = null)
    {
        var page = await _repo.GetByIdAsync(id);
        if (page == null) return null;

        var version = await _repo.GetVersionAsync(id, versionNumber);
        if (version == null) return null;

        var snapshot = JsonSerializer.Deserialize<PageSnapshot>(version.SnapshotJson);
        if (snapshot == null) return null;

        page.Title = snapshot.Title;
        page.Slug = snapshot.Slug;
        page.Description = snapshot.Description;
        page.ScreenType = snapshot.ScreenType;
        page.LifecycleState = snapshot.LifecycleState;
        page.IsActive = snapshot.IsActive;
        page.CurrentVersion++;

        page.PageWidgets.Clear();
        foreach (var item in snapshot.Widgets.OrderBy(w => w.Position))
        {
            page.PageWidgets.Add(new PageWidget
            {
                WidgetId = item.WidgetId,
                Position = item.Position,
                Width = item.Width
            });
        }

        var updated = await _repo.UpdateAsync(page);
        await SaveVersionAsync(updated, userId, "Rollback", dto?.Note ?? $"Rollback to v{versionNumber}");
        return MapToDto(updated);
    }

    public async Task<IEnumerable<PageVersionDto>> GetVersionsAsync(int id)
    {
        var versions = await _repo.GetVersionsAsync(id);
        return versions.Select(v => new PageVersionDto
        {
            Id = v.Id,
            PageId = v.PageId,
            VersionNumber = v.VersionNumber,
            Action = v.Action,
            Note = v.Note,
            CreatedBy = v.CreatedBy,
            CreatedAt = v.CreatedAt
        });
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var page = await _repo.GetByIdAsync(id);
        if (page == null) return false;
        await _repo.DeleteAsync(id);
        return true;
    }

    public Task AddWidgetAsync(int pageId, int widgetId, int position, int width)
        => _repo.AddWidgetAsync(pageId, widgetId, position, width);

    public Task RemoveWidgetAsync(int pageId, int widgetId)
        => _repo.RemoveWidgetAsync(pageId, widgetId);

    public Task UpdateWidgetLayoutAsync(int pageId, int widgetId, int position, int width)
        => _repo.UpdateWidgetLayoutAsync(pageId, widgetId, position, width);

    private async Task SaveVersionAsync(Page page, string createdBy, string action, string? note)
    {
        var snapshot = new PageSnapshot
        {
            Title = page.Title,
            Slug = page.Slug,
            Description = page.Description,
            ScreenType = page.ScreenType,
            LifecycleState = page.LifecycleState,
            IsActive = page.IsActive,
            Widgets = page.PageWidgets
                .OrderBy(x => x.Position)
                .Select(x => new PageWidgetSnapshot
                {
                    WidgetId = x.WidgetId,
                    Position = x.Position,
                    Width = x.Width
                })
                .ToList()
        };

        await _repo.CreateVersionAsync(new PageVersion
        {
            PageId = page.Id,
            TenantId = page.TenantId,
            VersionNumber = page.CurrentVersion,
            SnapshotJson = JsonSerializer.Serialize(snapshot),
            Action = action,
            Note = note,
            CreatedBy = createdBy
        });
    }

    private static PageDto MapToDto(Page page) => new()
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
                HtmlTemplate = pw.Widget?.HtmlTemplate,
                Configuration = pw.Widget?.Configuration,
                WidgetType = pw.Widget?.WidgetType.ToString() ?? string.Empty,
                Position = pw.Position,
                Width = pw.Width
            }).ToList()
    };

    private sealed class PageSnapshot
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ScreenType ScreenType { get; set; } = ScreenType.Frontend;
        public ScreenLifecycleState LifecycleState { get; set; } = ScreenLifecycleState.Draft;
        public bool IsActive { get; set; }
        public List<PageWidgetSnapshot> Widgets { get; set; } = new();
    }

    private sealed class PageWidgetSnapshot
    {
        public int WidgetId { get; set; }
        public int Position { get; set; }
        public int Width { get; set; }
    }
}
