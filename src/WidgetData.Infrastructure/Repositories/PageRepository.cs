using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Repositories;

public class PageRepository : IPageRepository
{
    private readonly ApplicationDbContext _context;

    public PageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Page>> GetAllByTenantAsync(int tenantId)
        => await _context.Pages
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId)
            .Include(p => p.PageWidgets).ThenInclude(pw => pw.Widget)
            .OrderBy(p => p.Title)
            .ToListAsync();

    public async Task<Page?> GetByIdAsync(int id)
        => await _context.Pages
            .Include(p => p.PageWidgets).ThenInclude(pw => pw.Widget)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Page?> GetBySlugAsync(string slug, int? tenantId = null)
    {
        var query = _context.Pages
            .IgnoreQueryFilters()
            .Include(p => p.PageWidgets).ThenInclude(pw => pw.Widget)
            .Where(p => p.Slug == slug && p.IsActive);

        if (tenantId.HasValue)
            query = query.Where(p => p.TenantId == tenantId.Value);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<Page> CreateAsync(Page page)
    {
        _context.Pages.Add(page);
        await _context.SaveChangesAsync();
        return page;
    }

    public async Task<Page> UpdateAsync(Page page)
    {
        page.UpdatedAt = DateTime.UtcNow;
        _context.Pages.Update(page);
        await _context.SaveChangesAsync();
        return page;
    }

    public async Task DeleteAsync(int id)
    {
        var page = await _context.Pages.FindAsync(id);
        if (page != null)
        {
            _context.Pages.Remove(page);
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddWidgetAsync(int pageId, int widgetId, int position, int width)
    {
        // Avoid duplicates
        var existing = await _context.PageWidgets
            .FirstOrDefaultAsync(pw => pw.PageId == pageId && pw.WidgetId == widgetId);
        if (existing != null)
        {
            existing.Position = position;
            existing.Width = width;
        }
        else
        {
            _context.PageWidgets.Add(new PageWidget
            {
                PageId = pageId,
                WidgetId = widgetId,
                Position = position,
                Width = width
            });
        }
        await _context.SaveChangesAsync();
    }

    public async Task RemoveWidgetAsync(int pageId, int widgetId)
    {
        var pw = await _context.PageWidgets
            .FirstOrDefaultAsync(x => x.PageId == pageId && x.WidgetId == widgetId);
        if (pw != null)
        {
            _context.PageWidgets.Remove(pw);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateWidgetLayoutAsync(int pageId, int widgetId, int position, int width)
    {
        var pw = await _context.PageWidgets
            .FirstOrDefaultAsync(x => x.PageId == pageId && x.WidgetId == widgetId);
        if (pw != null)
        {
            pw.Position = position;
            pw.Width = width;
            await _context.SaveChangesAsync();
        }
    }
}
