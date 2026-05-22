using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Repositories;

public class ExecutionRepository : IExecutionRepository
{
    private readonly ApplicationDbContext _context;

    public ExecutionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WidgetExecution>> GetAllAsync()
        => await _context.WidgetExecutions.AsNoTracking().ToListAsync();

    public Task<int> CountAsync()
        => _context.WidgetExecutions.AsNoTracking().CountAsync();

    public Task<int> CountByStatusAsync(ExecutionStatus status)
        => _context.WidgetExecutions.AsNoTracking().CountAsync(e => e.Status == status);

    public async Task<IEnumerable<WidgetExecution>> GetRecentAsync(int days, int limit)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return await _context.WidgetExecutions
            .AsNoTracking()
            .Include(e => e.Widget)
            .Where(e => e.StartedAt >= since)
            .OrderByDescending(e => e.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<ExecutionDashboardStats> GetDashboardStatsAsync(int days, int limit)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var statusCounts = await _context.WidgetExecutions
            .AsNoTracking()
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var total = statusCounts.Sum(x => x.Count);
        var successful = statusCounts.FirstOrDefault(x => x.Status == ExecutionStatus.Success)?.Count ?? 0;
        var failed = statusCounts.FirstOrDefault(x => x.Status == ExecutionStatus.Failed)?.Count ?? 0;

        var recent = await _context.WidgetExecutions
            .AsNoTracking()
            .Include(e => e.Widget)
            .Where(e => e.StartedAt >= since)
            .OrderByDescending(e => e.StartedAt)
            .Take(limit)
            .ToListAsync();

        return new ExecutionDashboardStats(total, successful, failed, recent);
    }

    public async Task<IEnumerable<WidgetExecution>> GetByWidgetIdAsync(int widgetId)
        => await _context.WidgetExecutions.Where(e => e.WidgetId == widgetId)
            .OrderByDescending(e => e.StartedAt).Take(100).ToListAsync();

    public async Task<WidgetExecution?> GetByIdAsync(int id)
        => await _context.WidgetExecutions.FindAsync(id);

    public async Task<WidgetExecution> CreateAsync(WidgetExecution execution)
    {
        _context.WidgetExecutions.Add(execution);
        await _context.SaveChangesAsync();
        return execution;
    }

    public async Task<WidgetExecution> UpdateAsync(WidgetExecution execution)
    {
        _context.WidgetExecutions.Update(execution);
        await _context.SaveChangesAsync();
        return execution;
    }
}
