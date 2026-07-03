using System.Text.Json;
using Microsoft.Extensions.Logging;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class PageVersioningService : IPageVersioningService
{
    private readonly IPageRepository _repo;
    private readonly ILogger _logger;

    public PageVersioningService(IPageRepository repo, ILogger logger)
    {
        _repo = repo;
        _logger = logger;
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
        await SaveSnapshotAsync(updated, userId, "Published", dto?.Note ?? "Published screen");
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
        await SaveSnapshotAsync(updated, userId, "Rollback", dto?.Note ?? $"Rollback to v{versionNumber}");
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

    public async Task SaveSnapshotAsync(Page page, string createdBy, string action, string? note)
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
                HtmlTemplate = null,
                Configuration = null,
                WidgetType = pw.Widget?.WidgetType.ToString() ?? string.Empty,
                Position = pw.Position,
                Width = pw.Width
            }).ToList()
    };

    public sealed class PageSnapshot
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ScreenType ScreenType { get; set; } = ScreenType.Frontend;
        public ScreenLifecycleState LifecycleState { get; set; } = ScreenLifecycleState.Draft;
        public bool IsActive { get; set; }
        public List<PageWidgetSnapshot> Widgets { get; set; } = new();
    }

    public sealed class PageWidgetSnapshot
    {
        public int WidgetId { get; set; }
        public int Position { get; set; }
        public int Width { get; set; }
    }
}