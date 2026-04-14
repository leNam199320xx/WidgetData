using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Repositories;

public class WidgetConfigArchiveRepository : IWidgetConfigArchiveRepository
{
    private readonly ApplicationDbContext _context;

    public WidgetConfigArchiveRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WidgetConfigArchive>> GetAllAsync()
        => await _context.WidgetConfigArchives
            .Include(a => a.Widget)
            .OrderByDescending(a => a.ArchivedAt)
            .ToListAsync();

    public async Task<IEnumerable<WidgetConfigArchive>> GetByWidgetIdAsync(int widgetId)
        => await _context.WidgetConfigArchives
            .Where(a => a.WidgetId == widgetId)
            .OrderByDescending(a => a.ArchivedAt)
            .ToListAsync();

    public async Task<WidgetConfigArchive?> GetByIdAsync(int id)
        => await _context.WidgetConfigArchives.FindAsync(id);

    public async Task<WidgetConfigArchive> CreateAsync(WidgetConfigArchive archive)
    {
        _context.WidgetConfigArchives.Add(archive);
        await _context.SaveChangesAsync();
        return archive;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var archive = await _context.WidgetConfigArchives.FindAsync(id);
        if (archive == null) return false;
        _context.WidgetConfigArchives.Remove(archive);
        await _context.SaveChangesAsync();
        return true;
    }
}
