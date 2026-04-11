using Microsoft.EntityFrameworkCore;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Enums;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var now = DateTime.UtcNow.AddDays(-7);
        var recentExecutions = await _context.WidgetExecutions
            .Include(e => e.Widget)
            .Where(e => e.StartedAt >= now)
            .OrderByDescending(e => e.StartedAt)
            .Take(10)
            .ToListAsync();

        return new DashboardStatsDto
        {
            TotalWidgets = await _context.Widgets.CountAsync(),
            ActiveWidgets = await _context.Widgets.CountAsync(w => w.IsActive),
            TotalDataSources = await _context.DataSources.CountAsync(),
            ActiveDataSources = await _context.DataSources.CountAsync(d => d.IsActive),
            TotalSchedules = await _context.WidgetSchedules.CountAsync(),
            ActiveSchedules = await _context.WidgetSchedules.CountAsync(s => s.IsEnabled),
            TotalExecutions = await _context.WidgetExecutions.CountAsync(),
            SuccessfulExecutions = await _context.WidgetExecutions.CountAsync(e => e.Status == ExecutionStatus.Success),
            FailedExecutions = await _context.WidgetExecutions.CountAsync(e => e.Status == ExecutionStatus.Failed),
            RecentExecutions = recentExecutions.Select(e => new WidgetExecutionDto
            {
                Id = e.Id,
                ExecutionId = e.ExecutionId,
                WidgetId = e.WidgetId,
                WidgetName = e.Widget?.Name,
                Status = e.Status,
                TriggeredBy = e.TriggeredBy,
                RowCount = e.RowCount,
                ExecutionTimeMs = e.ExecutionTimeMs,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt
            }).ToList()
        };
    }
}
