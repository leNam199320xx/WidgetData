using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IWidgetRepository _widgetRepo;
    private readonly IDataSourceRepository _dataSourceRepo;
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IExecutionRepository _executionRepo;

    public DashboardService(
        IWidgetRepository widgetRepo,
        IDataSourceRepository dataSourceRepo,
        IScheduleRepository scheduleRepo,
        IExecutionRepository executionRepo)
    {
        _widgetRepo = widgetRepo;
        _dataSourceRepo = dataSourceRepo;
        _scheduleRepo = scheduleRepo;
        _executionRepo = executionRepo;
    }

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var widgets = (await _widgetRepo.GetAllAsync()).ToList();
        var dataSources = (await _dataSourceRepo.GetAllAsync()).ToList();
        var schedules = (await _scheduleRepo.GetAllAsync()).ToList();
        var allExecutions = (await _executionRepo.GetAllAsync()).ToList();
        var recentExecutions = (await _executionRepo.GetRecentAsync(7, 10)).ToList();

        return new DashboardStatsDto
        {
            TotalWidgets = widgets.Count,
            ActiveWidgets = widgets.Count(w => w.IsActive),
            TotalDataSources = dataSources.Count,
            ActiveDataSources = dataSources.Count(d => d.IsActive),
            TotalSchedules = schedules.Count(),
            ActiveSchedules = schedules.Count(s => s.IsEnabled),
            TotalExecutions = allExecutions.Count,
            SuccessfulExecutions = allExecutions.Count(e => e.Status == ExecutionStatus.Success),
            FailedExecutions = allExecutions.Count(e => e.Status == ExecutionStatus.Failed),
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
