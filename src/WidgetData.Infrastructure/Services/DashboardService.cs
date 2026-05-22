using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
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
        var widgetCountsTask = _widgetRepo.GetCountsAsync();
        var dataSourceCountsTask = _dataSourceRepo.GetCountsAsync();
        var scheduleCountsTask = _scheduleRepo.GetCountsAsync();
        var execStatsTask = _executionRepo.GetDashboardStatsAsync(7, 10);

        await Task.WhenAll(widgetCountsTask, dataSourceCountsTask, scheduleCountsTask, execStatsTask);

        var (totalWidgets, activeWidgets) = await widgetCountsTask;
        var (totalDataSources, activeDataSources) = await dataSourceCountsTask;
        var (totalSchedules, activeSchedules) = await scheduleCountsTask;
        var execStats = await execStatsTask;

        return new DashboardStatsDto
        {
            TotalWidgets = totalWidgets,
            ActiveWidgets = activeWidgets,
            TotalDataSources = totalDataSources,
            ActiveDataSources = activeDataSources,
            TotalSchedules = totalSchedules,
            ActiveSchedules = activeSchedules,
            TotalExecutions = execStats.Total,
            SuccessfulExecutions = execStats.Successful,
            FailedExecutions = execStats.Failed,
            RecentExecutions = execStats.Recent.Select(e => new WidgetExecutionDto
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
