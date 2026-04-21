using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Repositories;

public class WidgetActivityRepository : IWidgetActivityRepository
{
    private readonly ApplicationDbContext _context;

    public WidgetActivityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WidgetApiActivity> RecordAsync(WidgetApiActivity activity)
    {
        _context.WidgetApiActivities.Add(activity);
        await _context.SaveChangesAsync();
        return activity;
    }

    public async Task<IEnumerable<WidgetApiActivity>> GetByWidgetIdAsync(int widgetId, int page, int pageSize)
        => await _context.WidgetApiActivities
            .Where(a => a.WidgetId == widgetId)
            .OrderByDescending(a => a.CalledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> CountByWidgetIdAsync(int widgetId)
        => await _context.WidgetApiActivities.CountAsync(a => a.WidgetId == widgetId);

    public async Task<IEnumerable<WidgetApiActivity>> GetSummaryDataAsync(int widgetId)
        => await _context.WidgetApiActivities
            .Where(a => a.WidgetId == widgetId)
            .ToListAsync();

    public async Task<IEnumerable<Widget>> GetInactiveWidgetsAsync(int thresholdDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-thresholdDays);
        return await _context.Widgets
            .Where(w => w.IsActive && (w.LastActivityAt == null || w.LastActivityAt < cutoff))
            .ToListAsync();
    }
}
