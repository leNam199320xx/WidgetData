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
        var recentExecutions = (await _executionRepo.GetRecentAsync(7, 10)).ToList();

        return new DashboardStatsDto
        {
            TotalWidgets = await _widgetRepo.CountAsync(),
            ActiveWidgets = await _widgetRepo.CountActiveAsync(),
            TotalDataSources = await _dataSourceRepo.CountAsync(),
            ActiveDataSources = await _dataSourceRepo.CountActiveAsync(),
            TotalSchedules = await _scheduleRepo.CountAsync(),
            ActiveSchedules = await _scheduleRepo.CountEnabledAsync(),
            TotalExecutions = await _executionRepo.CountAsync(),
            SuccessfulExecutions = await _executionRepo.CountByStatusAsync(ExecutionStatus.Success),
            FailedExecutions = await _executionRepo.CountByStatusAsync(ExecutionStatus.Failed),
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
