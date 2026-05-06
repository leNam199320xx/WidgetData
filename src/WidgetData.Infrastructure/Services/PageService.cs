using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class PageService : IPageService
{
    private readonly IPageRepository _repo;

    public PageService(IPageRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<PageDto>> GetAllAsync(int tenantId)
    {
        var pages = await _repo.GetAllByTenantAsync(tenantId);
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
            IsActive = true,
            CreatedBy = createdBy
        };
        var created = await _repo.CreateAsync(page);
        return MapToDto(created);
    }

    public async Task<PageDto?> UpdateAsync(int id, UpdatePageDto dto)
    {
        var page = await _repo.GetByIdAsync(id);
        if (page == null) return null;

        page.Title = dto.Title;
        page.Slug = dto.Slug.ToLowerInvariant();
        page.Description = dto.Description;
        page.IsActive = dto.IsActive;
        var updated = await _repo.UpdateAsync(page);
        return MapToDto(updated);
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

    private static PageDto MapToDto(Page page) => new()
    {
        Id = page.Id,
        TenantId = page.TenantId,
        Title = page.Title,
        Slug = page.Slug,
        Description = page.Description,
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
}
